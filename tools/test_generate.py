# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""JIS X 0213所属フラグの生成規則を検証します。"""

import unittest

from generate import (
    JIS_X_0208,
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


if __name__ == '__main__':
    unittest.main()
