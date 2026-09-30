# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定したMJ文字情報一覧表からMJ文字情報API用の表を生成する。"""

import argparse
from collections import Counter
import json
from pathlib import Path
import re

from generate import DATA, mj_rows, registered

OUTPUT = Path(__file__).resolve().parent.parent / 'KanjiVariants/Generated/GeneratedMjCharacterData.g.cs'
COLUMNS = ('C', 'D', 'E', 'F', 'G', 'N', 'L', 'M')
POLICIES = {'': '', '常用漢字': 'Joyo', '人名用漢字': 'Jinmeiyo'}


def valid_scalar(cp):
    return 0 <= cp <= 0x10FFFF and not 0xD800 <= cp <= 0xDFFF


def parse():
    rows = list(mj_rows())
    names = set()
    stats = Counter()
    registered_keys = set(registered(DATA / 'IVD_Sequences.txt')) | set(registered(DATA / 'StandardizedVariants.txt'))
    for row in rows:
        name = row.get('C', '')
        if not re.fullmatch(r'MJ[0-9]{6}', name) or name in names:
            raise ValueError(f'MJ文字図形名が空、不正または重複しています: {name!r}')
        names.add(name)
        for col in ('D', 'E', 'M'):
            value = row.get(col, '')
            if value and (not re.fullmatch(r'U\+[0-9A-Fa-f]{4,6}', value) or not valid_scalar(int(value[2:], 16))):
                raise ValueError(f'{name}: {col}列のUCSが不正です: {value!r}')
        for col in ('F', 'G'):
            value = row.get(col, '')
            if not value:
                continue
            # 原典のセミコロン区切りを両列で扱い、順番を変えず全シーケンスを検証します。
            sequences = value.split(';')
            for sequence in sequences:
                if not re.fullmatch(r'[0-9A-Fa-f]{4,6}_[0-9A-Fa-f]{4,6}', sequence):
                    raise ValueError(f'{name}: {col}列のシーケンスが不正です: {sequence!r}')
                base, vs = (int(part, 16) for part in sequence.split('_'))
                selector_valid = 0xE0100 <= vs <= 0xE01EF if col == 'F' else 0xFE00 <= vs <= 0xFE0F
                if not valid_scalar(base) or not selector_valid or (base | (vs << 21)) not in registered_keys:
                    raise ValueError(f'{name}: {col}列が有効な登録済みIVS/SVSではありません: {sequence!r}')
            stats[col + '_sequences'] += len(sequences)
            if len(sequences) > 1:
                stats[col + '_multiple_rows'] += 1
        if row.get('L', '') not in POLICIES:
            raise ValueError(f'{name}: 未知の漢字施策: {row["L"]!r}')
        for col in COLUMNS:
            value = row.get(col, '')
            if any(c in value for c in '\t\r\n'):
                raise ValueError(f'{name}: {col}列に区切り文字があります')
            if value:
                stats[col] += 1
        if row.get('L'):
            stats[row['L']] += 1
    if len(rows) != 58862:
        raise ValueError(f'MJ文字件数が変化しています: {len(rows)}')
    return sorted(rows, key=lambda row: row['C']), stats


def generate(rows):
    out = [
        '// 自動生成ファイルです。手編集しないでください。',
        '// 収録データはMJ文字情報一覧表 Ver.006.02を元に加工しています。',
        '// 出典・利用条件の詳細は LICENSE-NOTICES.md を参照してください。',
        '', 'namespace KanjiVariants;', '',
        'internal static class GeneratedMjCharacterData', '{',
        '    // 列順: MJ文字図形名、対応UCS、実装UCS、IVS、SVS、X0213、漢字施策、互換漢字。',
        '    // 分割した定数で、大量のレコード初期化による巨大なメソッドを避けます。',
        '    internal static readonly string[] Parts = new string[]', '    {',
    ]
    for start in range(0, len(rows), 500):
        values = []
        for row in rows[start:start + 500]:
            fields = [POLICIES[row.get(col, '')] if col == 'L' else row.get(col, '') for col in COLUMNS]
            values.append('\t'.join(fields))
        out.append('        ' + json.dumps('\n'.join(values), ensure_ascii=True) + ',')
    out.extend(['    };', '}', ''])
    return '\n'.join(out)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    rows, stats = parse()
    result = generate(rows)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding='utf-8') != result:
            raise SystemExit('生成済みのMJ文字情報データが原典と一致しません')
    else:
        OUTPUT.write_text(result, encoding='utf-8', newline='\n')
    print(f'MJ文字件数={len(rows)} MJ文字図形名ユニーク={stats["C"]} 列別件数={dict(stats)}')


if __name__ == '__main__':
    main()
