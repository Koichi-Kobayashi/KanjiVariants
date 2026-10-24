# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""既存MJ縮退原典から、直接関係だけの根拠付き逆索引を生成します。"""
import argparse
import json
from pathlib import Path

from generate import DATA, array, key, mj_rows, registered, scalar, variations

OUTPUT = Path(__file__).resolve().parents[1] / 'KanjiVariants/Generated/GeneratedMjFallbackData.g.cs'
GROUPS = (
    'JIS包摂規準・UCS統合規則', '法務省戸籍法関連通達・通知',
    '法務省告示582号別表第四', '辞書類等による関連字', '読み・字形による類推',
)


def build_index(rows, shrink, valid):
    """target -> source -> 根拠集合。元表現の列由来も保持し、推移展開しません。"""
    by_name = {row['C']: row for row in rows}
    if len(by_name) != len(rows):
        raise ValueError('MJ一覧に識別子の重複があります')
    seen = set()
    index = {}
    for item in shrink:
        name = item['MJ文字図形名']
        if name in seen or name not in by_name:
            raise ValueError(f'MJ縮退表の識別子が重複または不明です: {name}')
        seen.add(name)
        row = by_name[name]
        representations = {}
        for column, kind in (('E', 1), ('M', 16)):
            cp = scalar(row.get(column))
            if cp is not None:
                representations[cp] = representations.get(cp, 0) | kind
        # 対応UCSを実装UCSと同一視しない。既存CSVと同じ補助表現規則を明記して保持。
        if not representations:
            cp = scalar(row.get('D'))
            if cp is not None:
                representations[cp] = 2
        for column, kind in (('F', 4), ('G', 8)):
            for base, selector in variations(row.get(column)):
                packed = key(base, selector)
                if packed not in valid:
                    raise ValueError(f'未登録Variation Sequence: {name}')
                representations[packed] = representations.get(packed, 0) | kind
        unknown = set(item) - {'MJ文字図形名', '参考情報', *GROUPS}
        if unknown:
            raise ValueError(f'未知の関係分類: {unknown}')
        for kind, group in enumerate(GROUPS):
            for relation in item.get(group, []):
                unknown_fields = set(relation) - {'UCS', 'JIS X 0213', '種別', 'ホップ数', '付記', '表', '順位'}
                if unknown_fields:
                    raise ValueError(f'未知の根拠フィールド: {unknown_fields}')
                target = scalar(relation.get('UCS'))
                if target is None or target == 0xFF3F:
                    continue
                for source, representation_kind in representations.items():
                    if source == target:
                        continue
                    evidence = (int(name[2:]), kind, representation_kind,
                                relation.get('JIS X 0213', ''), relation.get('種別', ''),
                                relation.get('ホップ数', -1), relation.get('付記', ''),
                                relation.get('表', ''), relation.get('順位', ''))
                    index.setdefault(target, {}).setdefault(source, set()).add(evidence)
    if seen != set(by_name):
        raise ValueError('MJ一覧と縮退表の識別子集合が一致しません')
    return index


def generate(index):
    evidence = sorted({e for sources in index.values() for values in sources.values() for e in values})
    evidence_ids = {e: i for i, e in enumerate(evidence)}
    strings = sorted({'', *(e[i] for e in evidence for i in (3, 4, 6, 7, 8))})
    string_ids = {s: i for i, s in enumerate(strings)}
    targets, starts, sources, evidence_starts, references = [], [0], [], [0], []
    for target, candidates in sorted(index.items()):
        targets.append(target)
        # コードポイント、次にVSの順。packed keyの整数順とは異なる。
        for source, values in sorted(candidates.items(), key=lambda p: (p[0] & 0x1FFFFF, p[0] >> 21)):
            sources.append(source)
            references.extend(sorted(evidence_ids[e] for e in values))
            evidence_starts.append(len(references))
        starts.append(len(sources))
    records = []
    for e in evidence:
        records.extend((e[0], e[1], e[2], string_ids[e[3]], string_ids[e[4]], e[5],
                        string_ids[e[6]], string_ids[e[7]], string_ids[e[8]]))
    text_starts = [0]
    for value in strings:
        text_starts.append(text_starts[-1] + len(value.encode('utf-16-le')) // 2)
    out = [
        '// 自動生成。手編集しないでください。',
        '// 原著作者: IPA。MJ一覧 Ver.006.02 / MJ縮退マップ Ver.1.2.0。',
        '// 加工者: Koichi Kobayashi。直接縮退関係を元表現・根拠付きの逆索引に再構成。',
        '// MJ加工データ: CC BY-SA 2.1 JP。独自プログラムのMITとは別です。',
        '// https://creativecommons.org/licenses/by-sa/2.1/jp/ / LICENSE-NOTICES.md。',
        '// 登録シーケンス照合: 固定Unicode IVD/UCD、Unicode License V3。',
        'namespace KanjiVariants;', '', 'internal static class GeneratedMjFallbackData', '{',
        '    // 専用型の初期化は新APIを使用するときだけ。既存APIから参照しない。',
        '    static GeneratedMjFallbackData() { }',
        '    // EvidenceData: MJ番号、分類、表現由来、JIS文字列ID、通知種別ID、ホップ数(-1なし)、付記ID、表ID、順位ID。',
    ]
    for name, values, typ in (
        ('Targets', targets, 'int'), ('Starts', starts, 'int'), ('Sources', sources, 'ulong'),
        ('EvidenceStarts', evidence_starts, 'int'), ('EvidenceReferences', references, 'int'),
        ('EvidenceData', records, 'int'),
        ('TextStarts', text_starts, 'int'),
    ):
        out.append('    ' + array(name, values, typ))
    # 数千の文字列をcctorで配列化しない。検索した根拠の文字列だけを実行時に切り出す。
    text = ''.join(strings)
    out.append('    internal const string Text =\n        ' +
               ' +\n        '.join(json.dumps(text[i:i + 1000], ensure_ascii=True)
                                     for i in range(0, len(text), 1000)) + ';')
    out.append('}')
    return '\n\n'.join(out) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    rows = list(mj_rows())
    shrink = json.loads((DATA / 'MJ/MJShrinkMap.1.2.0.json').read_text(encoding='utf-8'))['content']
    valid = set(registered(DATA / 'Unicode/IVD_Sequences.txt')) | set(registered(DATA / 'Unicode/StandardizedVariants.txt'))
    result = generate(build_index(rows, shrink, valid))
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding='utf-8') != result:
            raise SystemExit('MJ縮退逆索引が原典と一致しません')
    else:
        OUTPUT.write_text(result, encoding='utf-8')
    print('MJ fallback reverse index: ' + ('matches' if args.check else 'generated'))


if __name__ == '__main__':
    main()
