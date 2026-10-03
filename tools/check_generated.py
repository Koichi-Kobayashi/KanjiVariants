# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定原典からの生成結果を、管理中のC#と派生CSVに書き込まず検証する。"""

from pathlib import Path
import subprocess
import sys
import tempfile
from unittest.mock import patch

import generate


def main():
    root = Path(__file__).resolve().parents[1]
    # --checkがない既存Generatorは出力先だけ退避し、生成処理をそのまま使います。
    # read_textは改行を正規化するため、Windows/Linuxの改行差で誤検知しません。
    expected_cs = generate.OUT
    expected_csv = generate.UNIQUE_CSV
    with tempfile.TemporaryDirectory(prefix='kanjivariants-generated-') as directory:
        temporary = Path(directory)
        actual_cs = temporary / expected_cs.name
        actual_csv = temporary / expected_csv.name
        with patch.object(generate, 'OUT', actual_cs), patch.object(generate, 'UNIQUE_CSV', actual_csv):
            generate.main()
        for expected, actual in ((expected_cs, actual_cs), (expected_csv, actual_csv)):
            if expected.read_text(encoding='utf-8') != actual.read_text(encoding='utf-8'):
                raise SystemExit(f'生成結果が一致しません: {expected}')

    # 既存のcheck実装には入力検証もあるため、同じ経路を呼び出します。
    for script in (
        'generate_joyo.py',
        'generate_education_kanji.py',
        'generate_jinmeiyo.py',
        'generate_joyo_education.py',
        'generate_okurigana.py',
        'generate_mj_character.py',
        'generate_jis_x0212.py',
    ):
        subprocess.run([sys.executable, str(root / 'tools' / script), '--check'],
                       cwd=root, check=True)
    print('Generated check: all 8 generators and derived MJ CSV match.')


if __name__ == '__main__':
    main()
