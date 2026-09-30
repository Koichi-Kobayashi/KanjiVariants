# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定したMJ文字情報一覧表から人名用漢字の検索表を生成する。"""

from __future__ import annotations

import argparse
from pathlib import Path
import re

from generate import mj_rows


ROOT = Path(__file__).resolve().parent.parent
OUTPUT = ROOT / "KanjiVariants" / "Generated" / "GeneratedJinmeiyoKanjiData.g.cs"
EXPECTED_COUNT = 863
UCS_PATTERN = re.compile(r"U\+[0-9A-Fa-f]{4,6}\Z")


def parse() -> list[int]:
    # L列は漢字施策、C列はMJ文字図形名、E列は実装したUCSです。
    selected = [row for row in mj_rows() if row.get("L") == "人名用漢字"]
    if len(selected) != EXPECTED_COUNT:
        raise ValueError(f"人名用漢字が{len(selected)}件です。期待値は{EXPECTED_COUNT}件")

    names: set[str] = set()
    code_points: set[int] = set()
    for row in selected:
        name = row.get("C")
        if not name or name in names:
            raise ValueError(f"MJ文字図形名が空または重複しています: {name!r}")
        names.add(name)

        value = row.get("E")
        if not value or not UCS_PATTERN.fullmatch(value):
            raise ValueError(f"{name}: 実装したUCSが単一コードポイントではありません: {value!r}")
        cp = int(value[2:], 16)
        if (cp > 0x10FFFF or 0xD800 <= cp <= 0xDFFF or
                0x180B <= cp <= 0x180D or cp == 0x180F or
                0xFE00 <= cp <= 0xFE0F or 0xE0100 <= cp <= 0xE01EF):
            raise ValueError(f"{name}: 実装したUCSが有効な漢字スカラーではありません: {value}")
        if cp in code_points:
            raise ValueError(f"{name}: 実装したUCSが重複しています: {value}")
        code_points.add(cp)

    if len(names) != EXPECTED_COUNT or len(code_points) != EXPECTED_COUNT:
        raise ValueError("MJ文字図形名または実装したUCSのユニーク件数が不正です")
    return sorted(code_points)


def generate(code_points: list[int]) -> str:
    lines = [
        "// 自動生成ファイルです。手編集しないでください。",
        "// 収録データはMJ文字情報一覧表の「人名用漢字」を元に加工しています。",
        "// 出典・利用条件の詳細は LICENSE-NOTICES.md を参照してください。",
        "",
        "namespace KanjiVariants;",
        "",
        "internal static class GeneratedJinmeiyoKanjiData",
        "{",
        "    // 「実装したUCS」を二分探索できるようコードポイント順に保持します。",
        "    internal static readonly int[] CodePoints = new int[]",
        "    {",
    ]
    for start in range(0, len(code_points), 12):
        lines.append("        " + ", ".join(f"0x{cp:X}" for cp in code_points[start:start + 12]) + ",")
    lines.extend(["    };", "}", ""])
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="生成結果と既存ファイルの一致を確認")
    args = parser.parse_args()
    code_points = parse()
    generated = generate(code_points)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf-8") != generated:
            raise SystemExit("生成済みの人名用漢字データが原典と一致しません")
    else:
        OUTPUT.write_text(generated, encoding="utf-8", newline="\n")
    print(f"人名用漢字={len(code_points)} MJ文字図形名ユニーク={EXPECTED_COUNT} 実装したUCSユニーク={len(code_points)}")


if __name__ == "__main__":
    main()
