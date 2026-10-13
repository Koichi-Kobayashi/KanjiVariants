# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""JIS X 0213所属フラグの生成規則を検証します。"""

import unittest
import hashlib
import tempfile
from pathlib import Path

from generate import (
    JIS_X_0208,
    load_jis_x0208_mapping,
    JIS_X_0213_PLANE_1,
    JIS_X_0213_PLANE_2,
    character_set_flags,
    mj_rows,
    x0213_flag,
)


class CharacterSetGenerationTests(unittest.TestCase):
    def test_x0213_parse(self):
        self.assertEqual(JIS_X_0213_PLANE_1, x0213_flag('1-17-15'))
        self.assertEqual(JIS_X_0213_PLANE_2, x0213_flag('2-01-04'))
        self.assertEqual(0, x0213_flag(None))
        self.assertEqual(0, x0213_flag(''))

    def test_invalid_x0213_is_reported(self):
        for value in ('3-01-04', '1-00-04', '2-01-95', '1-1-15', '1-17', '1-17-15-extra'):
            with self.subTest(value=value), self.assertRaises(ValueError):
                x0213_flag(value)
        with self.assertRaisesRegex(ValueError, 'MJ_TEST'):
            character_set_flags([{'C': 'MJ_TEST', 'E': 'U+4E11', 'N': '1-00-04'}], set())

    def test_implemented_ucs_only_and_no_sequence_flags(self):
        rows = [
            {'C': 'MJ_A', 'D': 'U+4E11', 'E': 'U+4E11', 'F': '4E11_E0101', 'N': '1-17-15'},
            {'C': 'MJ_B', 'D': 'U+4E11', 'F': '4E11_E0102', 'N': '2-01-04'},
            {'C': 'MJ_C', 'D': 'U+2000B', 'E': 'U+2000B', 'G': '2000B_FE00', 'N': '2-01-05'},
        ]
        bmp, supplementary = character_set_flags(rows, {0x4E11})
        self.assertEqual(JIS_X_0208 | JIS_X_0213_PLANE_1, bmp[0x4E11])
        self.assertEqual(JIS_X_0213_PLANE_2, supplementary[0x2000B - 0x20000])
        # IVS/SVS列や同じ「対応するUCS」の別字形からは所属bitを作らない。
        self.assertEqual(0, bmp[0x4E12])

    def test_actual_ushi_rows_select_plane_one_only(self):
        names = {'MJ006315', 'MJ006318', 'MJ006320', 'MJ056824'}
        rows = [row for row in mj_rows() if row.get('C') in names]
        self.assertEqual(names, {row['C'] for row in rows})
        by_name = {row['C']: row for row in rows}
        self.assertEqual(('U+4E11', '1-17-15'),
                         (by_name['MJ006315'].get('E'), by_name['MJ006315'].get('N')))
        self.assertEqual(('2-01-04', '2-01-04', None),
                         tuple(by_name[name].get('N') for name in ('MJ006318', 'MJ006320', 'MJ056824')))
        self.assertTrue(all(by_name[name].get('E') is None
                            for name in ('MJ006318', 'MJ006320', 'MJ056824')))
        bmp, _ = character_set_flags(rows, set())
        self.assertEqual(JIS_X_0213_PLANE_1, bmp[0x4E11])


class JisX0208SourceTests(unittest.TestCase):
    def test_all_kuten_and_compatibility(self):
        mapping = load_jis_x0208_mapping()
        values = [mapping.get((ku, ten)) for ku in range(1, 95) for ten in range(1, 95)]
        self.assertEqual(8836, len(values))
        self.assertEqual(6879, len(mapping))
        self.assertEqual(1957, values.count(None))
        self.assertEqual(6879, len(set(mapping.values())))
        self.assertEqual(0xff3c, mapping[1, 32])
        self.assertEqual(0x301c, mapping[1, 33])
        self.assertNotIn((85, 1), mapping)

    def test_all_combined_bmp_flags_match_published_1_2_0(self):
        # NuGet 1.2.0 / 983fa8eの実DLLから独立監査で取得した全65,536 bytes。
        # JIS X 0208だけでなくMJ由来X0213 bitも変わらないことを固定する。
        bmp, _ = character_set_flags(mj_rows(), set(load_jis_x0208_mapping().values()))
        self.assertEqual(65536, len(bmp))
        self.assertEqual('e13c299a846ae1c670b3e5b656f682ba77ba8b39dc1191f7f0bee52c1ac4a710',
                         hashlib.sha256(bytes(bmp)).hexdigest())

    def test_modified_source_is_rejected(self):
        source = Path(__file__).resolve().parents[1] / 'data/JIS/JIS0208.TXT'
        with tempfile.TemporaryDirectory() as directory:
            changed = Path(directory) / 'JIS0208.TXT'
            changed.write_bytes(source.read_bytes() + b'\n')
            with self.assertRaisesRegex(ValueError, 'SHA-256'):
                load_jis_x0208_mapping(changed)


if __name__ == '__main__':
    unittest.main()
