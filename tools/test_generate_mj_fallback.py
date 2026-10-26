# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

import unittest

from generate import key
from generate_mj_fallback import GROUPS, build_index, generate


class MjFallbackGenerationTests(unittest.TestCase):
    def test_all_five_groups_metadata_duplicates_and_self(self):
        rows = [{'C': 'MJ000001', 'E': 'U+9AD9', 'M': 'U+9AD9', 'F': '9AD9_E0100'}]
        relation = {'UCS': 'U+9AD8', 'JIS X 0213': '1-25-66', '種別': '通知',
                    'ホップ数': 2, '付記': '補足', '表': '一', '順位': '第1順位'}
        item = {'MJ文字図形名': 'MJ000001', **{g: [relation, relation] for g in GROUPS}}
        index = build_index(rows, [item], {key(0x9AD9, 0xE0100)})
        self.assertEqual({0x9AD9, key(0x9AD9, 0xE0100)}, set(index[0x9AD8]))
        self.assertEqual(5, len(index[0x9AD8][0x9AD9]))
        self.assertEqual({17}, {e[2] for e in index[0x9AD8][0x9AD9]})
        self.assertEqual({('1-25-66', '通知', 2, '補足', '一', '第1順位')},
                         {e[3:] for e in index[0x9AD8][0x9AD9]})
        self.assertEqual({}, build_index([{'C': 'MJ000001', 'E': 'U+9AD8'}], [item], set()))

    def test_stable_generation_and_distinct_mj_evidence(self):
        rows = [{'C': 'MJ000001', 'E': 'U+9AD9'}, {'C': 'MJ000002', 'E': 'U+9AD9'}]
        items = [{'MJ文字図形名': r['C'], GROUPS[0]: [{'UCS': 'U+9AD8'}]} for r in rows]
        forward = build_index(rows, items, set())
        backward = build_index(rows[::-1], items[::-1], set())
        self.assertEqual(2, len(forward[0x9AD8][0x9AD9]))
        self.assertEqual(generate(forward), generate(backward))

    def test_no_transitive_expansion_and_no_candidate_sentinel(self):
        rows = [{'C': 'MJ000001', 'E': 'U+9AD9'}, {'C': 'MJ000002', 'E': 'U+9AD8'}]
        items = [{'MJ文字図形名': 'MJ000001', GROUPS[0]: [{'UCS': 'U+9AD8'}, {'UCS': 'U+FF3F'}]},
                 {'MJ文字図形名': 'MJ000002', GROUPS[0]: [{'UCS': 'U+4E00'}]}]
        index = build_index(rows, items, set())
        self.assertEqual({0x9AD8}, set(index[0x4E00]))
        self.assertNotIn(0xFF3F, index)

    def test_invalid_join_and_unregistered_sequence_fail(self):
        with self.assertRaises(ValueError):
            build_index([{'C': 'MJ000001'}], [], set())
        with self.assertRaises(ValueError):
            build_index([{'C': 'MJ000001', 'F': '9AD9_E0100'}], [{'MJ文字図形名': 'MJ000001'}], set())
        with self.assertRaises(ValueError):
            build_index([{'C': 'MJ000001'}], [{'MJ文字図形名': 'MJ000001', '未知': []}], set())


if __name__ == '__main__':
    unittest.main()
