# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""data/に固定した公式データから、実行時検索用の配列を生成します。

実行方法: python tools/generate.py
"""
from __future__ import annotations

import collections
import csv
import json
import re
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'data'
OUT = ROOT / 'KanjiVariants' / 'GeneratedData.g.cs'
UNIQUE_CSV = DATA / 'MJUniqueAlternatives.1.2.0.csv'
NS = '{http://purl.oclc.org/ooxml/spreadsheetml/main}'


def mj_rows():
    # 厳格OOXMLの共有文字列表を使い、巨大なワークシート本体は行ごとに読み込みます。
    with zipfile.ZipFile(DATA / 'mji.00602.xlsx') as book:
        strings = [''.join(t.text or '' for t in cell.iter(NS + 't'))
                   for cell in ET.fromstring(book.read('xl/sharedStrings.xml'))]
        with book.open('xl/worksheets/sheet1.xml') as sheet:
            for _, row in ET.iterparse(sheet, events=('end',)):
                if row.tag != NS + 'row':
                    continue
                cells = {}
                for cell in row.findall(NS + 'c'):
                    value = cell.find(NS + 'v')
                    if value is None or value.text is None:
                        continue
                    col = re.match(r'[A-Z]+', cell.attrib['r']).group()
                    cells[col] = strings[int(value.text)] if cell.attrib.get('t') == 's' else value.text
                if row.attrib['r'] != '1':
                    yield cells
                row.clear()


def scalar(value):
    # UCSが単一コードポイントでない値は、scalar検索表には登録しません。
    if not value or not re.fullmatch(r'U\+[0-9A-Fa-f]{4,6}', value):
        return None
    return int(value[2:], 16)


def variations(value):
    # MJ一覧のIVS/SVS表記（基底文字_VS）をコードポイントの組に変換します。
    if not value:
        return []
    return [(int(a, 16), int(b, 16)) for a, b in
            re.findall(r'([0-9A-Fa-f]{4,6})[_ ,]+([0-9A-Fa-f]{4,6})', value)]


def key(base, vs):
    return base | (vs << 21)


def export_unique_alternatives(mj, unique, valid_variations):
    """一意な変換先とMJ文字図形ごとの元表現をCSVに書き出します。"""
    rows = []
    for item in unique:
        destination = item['変換先']
        target_cp = scalar(destination.get('UCS'))
        # U+FF3Fは公式データで「候補なし」を表す値なので一覧から除外します。
        if target_cp is None or target_cp == 0xFF3F:
            continue
        mj_name = item['MJ文字図形名']
        source = mj[mj_name]
        representations = set()
        for column in ('E', 'M'):
            cp = scalar(source.get(column))
            if cp is not None:
                representations.add((cp, None))
        # 実装UCS等がない行だけ、MJ一覧の代表UCSを補助表現として使います。
        if not representations:
            cp = scalar(source.get('D'))
            if cp is not None:
                representations.add((cp, None))
        for column in ('F', 'G'):
            for base, selector in variations(source.get(column)):
                if key(base, selector) in valid_variations:
                    representations.add((base, selector))
        for base, selector in sorted(representations, key=lambda value: (value[0], value[1] or 0)):
            source_cps = f'U+{base:04X}'
            source_text = chr(base)
            kind = 'Unicode文字'
            if selector is not None:
                source_cps += f' U+{selector:04X}'
                source_text += chr(selector)
                kind = 'IVS/SVS'
            rows.append({
                'MJ文字図形名': mj_name,
                '表現種別': kind,
                '変換元UCS': source_cps,
                '変換元文字': source_text,
                '変換先UCS': f'U+{target_cp:04X}',
                '変換先文字': chr(target_cp),
                '変換先JIS X 0213': destination.get('JIS X 0213', ''),
            })

    columns = ('MJ文字図形名', '表現種別', '変換元UCS', '変換元文字',
               '変換先UCS', '変換先文字', '変換先JIS X 0213')
    rows.sort(key=lambda row: (row['MJ文字図形名'], row['変換元UCS']))
    # Excelでも日本語を開きやすいようUTF-8 BOM付きで保存します。
    with UNIQUE_CSV.open('w', encoding='utf-8-sig', newline='') as output:
        writer = csv.DictWriter(output, fieldnames=columns, lineterminator='\n')
        writer.writeheader()
        writer.writerows(rows)
    print(f'Generated {UNIQUE_CSV.name}: {len(rows)} source representations')


def registered(path):
    # Unicodeの公式一覧に載るVariation Sequenceだけを有効な組み合わせとして収録します。
    for line in path.read_text(encoding='utf-8-sig').splitlines():
        body = line.split('#', 1)[0].strip()
        if not body:
            continue
        field = body.split(';', 1)[0].strip().split()
        if len(field) == 2:
            base, vs = (int(x, 16) for x in field)
            if (0xFE00 <= vs <= 0xFE0F or 0xE0100 <= vs <= 0xE01EF):
                yield key(base, vs)


def array(name, values, typ='int'):
    values = list(values)
    lines = [f'    internal static readonly {typ}[] {name} = new {typ}[]', '    {']
    step = 20 if typ != 'ulong' else 8
    for i in range(0, len(values), step):
        lines.append('        ' + ', '.join(str(v) + ('UL' if typ == 'ulong' else '')
                                    for v in values[i:i+step]) + ',')
    lines.append('    };')
    return '\n'.join(lines)


ARRAY_COMMENTS = {
    'BmpEntryIndices': 'BMPコードポイントからMJ Entryを直接引く表。-1は未登録を示します。',
    'SupplementaryEntryIndices': '補助平面のコードポイントからMJ Entryを直接引く表。基点はU+20000です。',
    'JisX0208Flags': 'JIS X 0208に収録されるBMPコードポイントを1で示します。',
    'UniqueAlternatives': '一意な変換表の変換先。0は公式データの「候補なし」です。',
    'AlternativeStarts': '各Entryの候補範囲がAlternativeCodePoints内で始まる位置です。',
    'AlternativeCounts': '各Entryが持つ候補数です。',
    'AlternativeCodePoints': '複数候補を重複排除して連結したコードポイント配列です。',
    'VariationKeys': '登録済みIVS/SVSの複合キーを昇順に並べた配列です。',
    'VariationEntryIndices': 'VariationKeysと同じ位置に対応するMJ Entryです。',
    'ValidVariationKeys': 'MJ側のEntry有無にかかわらず、Unicodeに登録された有効なIVS/SVSです。',
}


def main():
    # 一覧・候補・一意変換表はMJ文字図形名で対応付けます。
    mj = {row['C']: row for row in mj_rows()}
    shrink = json.loads((DATA / 'MJShrinkMap.1.2.0.json').read_text(encoding='utf-8'))['content']
    unique = json.loads((DATA / 'MJSU.1.2.0.json').read_text(encoding='utf-8'))['content']
    assert len(mj) == len(shrink) == len(unique) == 58862
    unique_by_mj = {item['MJ文字図形名']: scalar(item['変換先']['UCS']) or 0 for item in unique}
    groups = ('JIS包摂規準・UCS統合規則', '法務省戸籍法関連通達・通知',
              '法務省告示582号別表第四', '辞書類等による関連字', '読み・字形による類推')

    # CP932の拡張文字を混ぜず、JIS X 0208の区点だけをEUC-JPでUnicode化します。
    jis = set()
    for ku in range(1, 95):
        for ten in range(1, 95):
            try:
                decoded = bytes((ku + 0xA0, ten + 0xA0)).decode('euc_jp')
            except UnicodeDecodeError:
                continue
            if len(decoded) == 1:
                jis.add(ord(decoded))
    # ASCIIは文字列API側の高速経路で無条件に通すため、この表には含めません。

    scalar_to_entry = {}
    variation_to_entry = {}
    fallback = collections.defaultdict(set)
    unique_values = []
    starts = []
    counts = []
    alternatives = []
    for index, item in enumerate(shrink):
        name = item['MJ文字図形名']
        row = mj[name]
        unique_cp = unique_by_mj[name]
        if unique_cp == 0xFF3F:
            # U+FF3Fは公式表で「候補なし」を表す番兵なので変換先には使いません。
            unique_cp = 0
        unique_values.append(unique_cp)
        # 根拠のある候補だけを採り、「参考情報」は変換候補に含めません。
        candidates = []
        for group in groups:
            for candidate in item.get(group, []):
                cp = scalar(candidate.get('UCS'))
                if cp and cp != 0xFF3F and cp not in candidates:
                    candidates.append(cp)
        assert len(candidates) <= 8
        starts.append(len(alternatives))
        counts.append(len(candidates))
        alternatives.extend(candidates)
        for col in ('E', 'M'):
            cp = scalar(row.get(col))
            if cp is not None:
                scalar_to_entry.setdefault(cp, index)
        for col in ('F', 'G'):
            for base, vs in variations(row.get(col)):
                variation_to_entry.setdefault(key(base, vs), index)
        cp = scalar(row.get('D'))
        if cp is not None:
            fallback[cp].add(index)
    # 対応UCSは重複し得るため、実装UCSがないときだけ一意に結び付く値を補完します。
    for cp, indices in fallback.items():
        if len(indices) == 1:
            scalar_to_entry.setdefault(cp, next(iter(indices)))

    # The registered sequences are valid even without a corresponding MJ entry.
    valid = set(registered(DATA / 'IVD_Sequences.txt'))
    valid.update(registered(DATA / 'StandardizedVariants.txt'))
    export_unique_alternatives(mj, unique, valid)
    variation_to_entry = {k: v for k, v in variation_to_entry.items() if k in valid}
    scalar_keys = sorted(scalar_to_entry)
    assert scalar_keys[-1] <= 0x323AF
    bmp = [scalar_to_entry.get(cp, -1) for cp in range(0x10000)]
    supplementary = [scalar_to_entry.get(cp, -1) for cp in range(0x20000, 0x323B0)]
    jis_flags = [int(cp in jis) for cp in range(0x10000)]
    variation_keys = sorted(variation_to_entry)
    lines = ['// Copyright (c) 2026 Koichi Kobayashi\n// Licensed under the MIT License.',
             '// <auto-generated />', '// データ出典：IPA MJ（CC BY-SA 2.1 JP）、Unicode IVD/UCD、EUC-JPによるJIS X 0208対応。',
             'namespace KanjiVariants;', '', 'internal static class GeneratedData', '{']
    tables = [
        ('BmpEntryIndices', bmp, 'int'),
        ('SupplementaryEntryIndices', supplementary, 'int'),
        ('JisX0208Flags', jis_flags, 'byte'),
        ('UniqueAlternatives', unique_values, 'int'),
        ('AlternativeStarts', starts, 'int'),
        ('AlternativeCounts', counts, 'byte'),
        ('AlternativeCodePoints', alternatives, 'int'),
        ('VariationKeys', variation_keys, 'ulong'),
        ('VariationEntryIndices', (variation_to_entry[k] for k in variation_keys), 'int'),
        ('ValidVariationKeys', sorted(valid), 'ulong'),
    ]
    # 実行時のJSON解析をなくすため、用途ごとの固定配列としてC#へ出力します。
    lines.extend('    // ' + ARRAY_COMMENTS[name] + '\n' + array(name, values, typ)
                 for name, values, typ in tables)
    lines.append('}')
    OUT.write_text('\n\n'.join(lines) + '\n', encoding='utf-8')
    print(f'Generated {OUT.name}: {len(scalar_keys)} scalars, {len(variation_keys)} MJ sequences, '
          f'{len(valid)} valid sequences, {len(alternatives)} alternatives, {len(jis)} JIS characters')


if __name__ == '__main__':
    main()
