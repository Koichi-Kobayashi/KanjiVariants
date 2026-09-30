# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定した平成29年告示版の配当表の転記からC#データを生成する。"""

from __future__ import annotations

import argparse
import hashlib
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parent.parent
SOURCE_URL = "https://www.mext.go.jp/content/20230120-mxt_kyoiku02-100002604_01.pdf"
SOURCE_PDF = ROOT / "data" / "EducationKanjiGradeTable.pdf"
TRANSCRIPTION = ROOT / "data" / "EducationKanjiGradeTable.txt"
OUTPUT = ROOT / "KanjiVariants" / "Generated" / "GeneratedEducationKanjiData.g.cs"
SOURCE_SHA256 = "6af90f134b243e44f9767c37ee3079fac092883fd6359b836a5733dd25b43902"
EXPECTED = {1: 80, 2: 160, 3: 200, 4: 202, 5: 193, 6: 191}


def parse() -> dict[int, list[str]]:
    if hashlib.sha256(SOURCE_PDF.read_bytes()).hexdigest() != SOURCE_SHA256:
        raise ValueError("原典PDFのハッシュが固定版と一致しません。転記を再照合してください")

    grades: dict[int, list[str]] = {}
    current: int | None = None
    for number, raw in enumerate(TRANSCRIPTION.read_text(encoding="utf-8").splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        match = re.fullmatch(r"Grade([1-6]):", line)
        if match:
            current = int(match.group(1))
            if current in grades:
                raise ValueError(f"{number}行目: 学年が重複しています")
            grades[current] = []
            continue
        if current is None or ":" in line or any(c.isspace() for c in line):
            raise ValueError(f"{number}行目: 解釈できない転記行 {line!r}")
        for character in line:
            cp = ord(character)
            if not (0x3400 <= cp <= 0x4DBF or 0x4E00 <= cp <= 0x9FFF or
                    0xF900 <= cp <= 0xFAFF or 0x20000 <= cp <= 0x323AF):
                raise ValueError(f"{number}行目: 単一の漢字Unicodeスカラーではありません: {character!r}")
            grades[current].append(character)

    if set(grades) != set(EXPECTED):
        raise ValueError("第1～第6学年の見出しが揃っていません")
    for grade, expected in EXPECTED.items():
        if len(grades[grade]) != expected:
            raise ValueError(f"Grade{grade}: {len(grades[grade])}字。期待値は{expected}字")
    all_characters = [character for grade in EXPECTED for character in grades[grade]]
    duplicates = len(all_characters) - len(set(all_characters))
    if len(all_characters) != 1026 or duplicates:
        raise ValueError(f"合計または重複数が不正です: total={len(all_characters)} duplicates={duplicates}")
    return grades


def generate(grades: dict[int, list[str]]) -> str:
    sorted_entries = sorted((ord(character), grade)
                            for grade, characters in grades.items() for character in characters)
    lines = [
        "// 自動生成ファイルです。手編集しないでください。",
        "// 収録データは文部科学省『学年別漢字配当表』を元に加工しています。",
        "// 出典・利用条件の詳細は LICENSE-NOTICES.md を参照してください。",
        "",
        "namespace KanjiVariants;",
        "",
        "internal static class GeneratedEducationKanjiData",
        "{",
        "    // 二分探索用にコードポイント順で保持します。",
        "    internal static readonly int[] CodePoints = new int[]",
        "    {",
    ]
    for start in range(0, len(sorted_entries), 12):
        lines.append("        " + ", ".join(f"0x{cp:X}" for cp, _ in sorted_entries[start:start + 12]) + ",")
    lines.extend(["    };", "", "    internal static readonly byte[] Grades = new byte[]", "    {"])
    for start in range(0, len(sorted_entries), 24):
        lines.append("        " + ", ".join(str(grade) for _, grade in sorted_entries[start:start + 24]) + ",")
    lines.extend(["    };", "", "    internal static IReadOnlyList<KanjiCharacter> GetByGrade(int grade) => GradeLists.Items[grade - 1];",
                  "", "    private static class GradeLists", "    {", "        // 一覧は原典の掲載順で一度だけ構築し、読み取り専用として共有します。",
                  "        internal static readonly IReadOnlyList<KanjiCharacter>[] Items = new IReadOnlyList<KanjiCharacter>[]", "        {"])
    for grade in EXPECTED:
        lines.append("            Array.AsReadOnly(new KanjiCharacter[]")
        lines.append("            {")
        characters = grades[grade]
        for start in range(0, len(characters), 8):
            lines.append("                " + ", ".join(f"KanjiCharacter.FromScalar(0x{ord(c):X})"
                                                 for c in characters[start:start + 8]) + ",")
        lines.extend(["            }),"])
    lines.extend(["        };", "    }", "}", ""])
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="生成結果と既存ファイルの一致を確認")
    args = parser.parse_args()
    grades = parse()
    generated = generate(grades)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf-8") != generated:
            raise SystemExit("生成ファイルが固定の配当表と一致しません")
    else:
        OUTPUT.write_text(generated, encoding="utf-8", newline="\n")
    print(f"source={SOURCE_URL}")
    for grade in EXPECTED:
        print(f"grade{grade}={len(grades[grade])}")
    print("total=1026")
    print("duplicates=0")


if __name__ == "__main__":
    main()
