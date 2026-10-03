# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""差分確認が失敗を検出し、検証対象を書き換えないことを確認する。"""

from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import check_generated
import generate


class GeneratedCheckTests(unittest.TestCase):
    def check_fixture(self, differing_file):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            cs = root / 'fixture.cs'
            csv = root / 'fixture.csv'
            cs.write_text('table\n', encoding='utf-8')
            csv.write_text('header\nvalue\n', encoding='utf-8')
            expected = {cs: cs.read_bytes(), csv: csv.read_bytes()}

            def generate_fixture():
                generate.OUT.write_text('changed\n' if differing_file == 'cs' else 'table\n', encoding='utf-8')
                generate.UNIQUE_CSV.write_text('changed\n' if differing_file == 'csv' else 'header\nvalue\n', encoding='utf-8')

            with patch.object(generate, 'OUT', cs), patch.object(generate, 'UNIQUE_CSV', csv), \
                 patch.object(generate, 'main', side_effect=generate_fixture), \
                 patch.object(check_generated.subprocess, 'run') as run:
                if differing_file:
                    with self.assertRaises(SystemExit):
                        check_generated.main()
                    run.assert_not_called()
                else:
                    check_generated.main()
                    self.assertEqual(7, run.call_count)
                    self.assertTrue(all(call.kwargs['check'] for call in run.call_args_list))
            self.assertEqual(expected, {cs: cs.read_bytes(), csv: csv.read_bytes()})

    def test_matches_run_existing_checks_without_writing_sources(self):
        self.check_fixture(None)

    def test_csharp_and_csv_mismatch_fail_without_writing_sources(self):
        for kind in ('cs', 'csv'):
            with self.subTest(kind=kind):
                self.check_fixture(kind)


if __name__ == '__main__':
    unittest.main()
