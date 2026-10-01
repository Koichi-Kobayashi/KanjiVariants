# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定したICU対応表とUnihan kJis1を照合し、JIS X 0212所属bit表を生成する。"""

import argparse
import hashlib
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'data'
OUTPUT = ROOT / 'KanjiVariants/Generated/GeneratedJisX0212Data.g.cs'
ICU_COMMIT = '61607c27732906d36c5bd4d23ecc092f89f53a2b'
INPUTS = {
    'JIS/jisx-212.ucm': 'a3ad8492609c8fc8766f64610ef298b71ac872051fad5cd88802509fd6ee5f7b',
    'Unicode/Unihan_OtherMappings.18.0.0.txt': 'fc7bbb8f923fa1e922a4d421aa15387d1c68250cc5e7619bdf81f575ab38472b',
}


def add(mapping, cp, ku, ten):
    if not 0 <= cp <= 0x10FFFF or 0xD800 <= cp <= 0xDFFF:
        raise ValueError(f'不正なUnicode scalar: U+{cp:X}')
    if not 1 <= ku <= 94 or not 1 <= ten <= 94:
        raise ValueError(f'不正な区点: {ku}-{ten}')
    if cp in mapping or (ku, ten) in mapping.values():
        raise ValueError(f'コードポイントまたは区点が重複: U+{cp:X}')
    mapping[cp] = (ku, ten)


def parse():
    for name, digest in INPUTS.items():
        if hashlib.sha256((DATA / name).read_bytes()).hexdigest() != digest:
            raise ValueError(f'{name}: 固定原典のSHA-256が一致しません')
    all_chars = {}
    in_charmap = False
    for line in (DATA / 'JIS/jisx-212.ucm').read_text(encoding='utf-8').splitlines():
        if line == 'CHARMAP':
            in_charmap = True
            continue
        if line == 'END CHARMAP':
            in_charmap = False
            continue
        if not in_charmap or not line.strip() or line.startswith('#'):
            continue
        match = re.fullmatch(r'<U([0-9A-F]{4,6})> \\x([0-9A-F]{2})\\x([0-9A-F]{2}) \|0', line)
        if match is None:
            raise ValueError(f'未対応のICUマッピング: {line!r}')
        cp, ku, ten = (int(value, 16) for value in match.groups())
        add(all_chars, cp, ku - 0x20, ten - 0x20)
    han = {}
    for line in (DATA / 'Unicode/Unihan_OtherMappings.18.0.0.txt').read_text(encoding='utf-8').splitlines():
        if '\tkJis1\t' not in line or line.startswith('#'):
            continue
        match = re.fullmatch(r'U\+([0-9A-F]{4,6})\tkJis1\t([0-9]{2})([0-9]{2})', line)
        if match is None:
            raise ValueError(f'不正なkJis1: {line!r}')
        cp, ku, ten = match.groups()
        add(han, int(cp, 16), int(ku), int(ten))
    if len(all_chars) != 6067 or len(han) != 5801:
        raise ValueError(f'原典件数が変化: 全文字={len(all_chars)}, 漢字={len(han)}')
    if {cp: kuten for cp, kuten in all_chars.items() if 0x4E00 <= cp <= 0x9FFF} != han:
        raise ValueError('ICUとUnihanの漢字・区点が一致しません')
    return all_chars, han


def generate(characters):
    # 一呼び出しあたり添字参照とbit検査だけで判定し、BMP外も同じ方式で扱えます。
    membership = [0] * ((max(characters) >> 5) + 1)
    for cp in characters:
        membership[cp >> 5] |= 1 << (cp & 31)
    lines = ['// 自動生成ファイルです。手編集しないでください。',
             '// ICU JIS X 0212対応表を加工し、漢字部分をUnihan 18.0.0 kJis1と照合しています。',
             '// 出典・著作権表示・利用条件は LICENSE-NOTICES.md を参照してください。',
             '', 'namespace KanjiVariants;', '', 'internal static class GeneratedJisX0212Data', '{',
             '    // Unicode scalarを32文字ずつ格納する所属bit表です。',
             '    internal static readonly uint[] Membership = new uint[]', '    {']
    for start in range(0, len(membership), 8):
        lines.append('        ' + ', '.join(f'0x{value:08X}u' for value in membership[start:start + 8]) + ',')
    lines.extend(['    };', '}', ''])
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    characters, han = parse()
    generated = generate(characters)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding='utf-8') != generated:
            raise SystemExit('JIS X 0212生成データが固定原典と一致しません')
    else:
        OUTPUT.write_text(generated, encoding='utf-8', newline='\n')
    print(f'JIS X 0212={len(characters)} 漢字={len(han)} 非漢字={len(characters)-len(han)} '
          f'BMP={sum(cp <= 0xFFFF for cp in characters)} BMP外={sum(cp > 0xFFFF for cp in characters)}')


if __name__ == '__main__':
    main()
