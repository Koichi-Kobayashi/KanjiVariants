# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""原典の異なる括弧用途を区別し、構造変更時に失敗することを確認する。"""

from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import generate_okurigana as generator


class GeneratorTests(unittest.TestCase):
    def test_related_words_and_readings_are_not_alternatives(self):
        entry = generator.terms("動かす〔動く〕", 2, "Principle", None)[0]
        self.assertEqual(entry.alternatives, ())
        self.assertIn("〔動く〕", entry.note)
        reading = generator.terms("掛（かかり）", 4, "Exception", None)[0]
        self.assertEqual(reading.alternatives, ())
        self.assertIn("読み注記", reading.note)

    def test_rule2_permitted_brackets_are_alternatives(self):
        entry = generator.terms("浮かぶ〔浮ぶ〕", 2, "Permitted", None, True)[0]
        self.assertEqual(entry.word, "浮かぶ")
        self.assertEqual(entry.alternatives, ("浮ぶ",))

    def test_malformed_example_fails(self):
        with self.assertRaises(ValueError):
            generator.terms("行う（行なう", 1, "Permitted", None, True)

    def test_changed_source_fails_before_parsing(self):
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)
            (source / "rule1.html").write_bytes(b"changed source")
            with patch.object(generator, "SOURCE", source), self.assertRaises(ValueError):
                generator.parse_page("rule1.html", 1)


if __name__ == "__main__":
    unittest.main()
