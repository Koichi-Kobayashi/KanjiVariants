# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定した文化庁HTMLから常用漢字の参照用C#データを生成する。"""

from __future__ import annotations

import argparse
from collections import Counter
from html.parser import HTMLParser
import json
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "data" / "JoyoKanjiOnkunIndex.html"
OUTPUT = ROOT / "KanjiVariants" / "GeneratedJoyoKanjiData.g.cs"
SOURCE_URL = "https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/kanji/joyokanjisakuin/index.html"


class IndexParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.in_table = False
        self.in_cell = False
        self.cell_depth = 0
        self.current_cell: list[str] = []
        self.current_row: list[str] | None = None
        self.rows: list[list[str]] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        if tag == "table" and dict(attrs).get("id") == "urlist":
            self.in_table = True
        if not self.in_table:
            return
        if tag == "tr":
            self.current_row = []
        elif tag == "td" and self.current_row is not None:
            self.in_cell = True
            self.cell_depth = 1
            self.current_cell = []
        elif tag == "br" and self.in_cell:
            self.current_cell.append("\n")
        elif self.in_cell:
            self.cell_depth += 1

    def handle_endtag(self, tag: str) -> None:
        if not self.in_table:
            return
        if self.in_cell and tag != "br":
            self.cell_depth -= 1
            if tag == "td" and self.cell_depth == 0:
                assert self.current_row is not None
                self.current_row.append("".join(self.current_cell))
                self.in_cell = False
        if tag == "tr" and self.current_row:
            self.rows.append(self.current_row)
            self.current_row = None
        if tag == "table":
            self.in_table = False

    def handle_data(self, data: str) -> None:
        if self.in_cell:
            self.current_cell.append(data)


def lines(text: str) -> list[str]:
    return [part.strip(" \t\r\n\u3000\xa0") for part in text.split("\n")]


def parse() -> tuple[list[dict], Counter]:
    parser = IndexParser()
    parser.feed(SOURCE.read_bytes().decode("cp932"))
    if len(parser.rows) != 2136:
        raise ValueError(f"想定外の本表行数: {len(parser.rows)}")

    entries: list[dict] = []
    seen: set[str] = set()
    stats: Counter = Counter()
    for number, cells in enumerate(parser.rows, 1):
        if len(cells) != 4:
            raise ValueError(f"{number}行目: 4列ではありません")
        label = re.sub(r"[ \t\r\n\u3000\xa0]+", "", cells[0])
        match = re.fullmatch(r"([^（）［］]+)(?:［([^］]+)］)?((?:（[^）]+）)*)", label)
        if not match or len(match.group(1)) != 1:
            raise ValueError(f"{number}行目: 未解釈の字体表記 {label!r}")
        character = match.group(1)
        old_forms = re.findall(r"（([^）]+)）", match.group(3))
        if any(len(form) != 1 for form in old_forms):
            raise ValueError(f"{number}行目: 複数字からなる旧字体 {label!r}")
        if character in seen:
            raise ValueError(f"重複する本表文字: {character}")
        seen.add(character)
        if old_forms:
            stats["old_form_entries"] += 1
        if len(old_forms) > 1:
            stats["multiple_old_forms"] += 1
        if match.group(2):
            stats["bracketed_labels"] += 1

        reading_lines = lines(cells[1])
        example_lines = lines(cells[2])
        # 原典の空行は独立した読みではない。語例があれば直前の読みに続く。
        readings: list[dict] = []
        pending_examples: list[str] = []
        for index in range(max(len(reading_lines), len(example_lines))):
            reading = reading_lines[index] if index < len(reading_lines) else ""
            example = example_lines[index] if index < len(example_lines) else ""
            examples = [word.strip() for word in example.split("，") if word.strip()]
            if reading:
                if any("\u30a1" <= ch <= "\u30f6" for ch in reading):
                    kind = "On"
                elif any("\u3041" <= ch <= "\u3096" for ch in reading):
                    kind = "Kun"
                else:
                    raise ValueError(f"{number}行目: 音訓を判定できません: {reading!r}")
                readings.append({"kind": kind, "reading": reading,
                                 "examples": pending_examples + examples})
                pending_examples = []
                stats[kind] += 1
            elif examples:
                if not readings:
                    raise ValueError(f"{number}行目: 対応する読みのない語例")
                following = next((j for j in range(index + 1, len(reading_lines))
                                  if reading_lines[j]), None)
                if following is not None and (following >= len(example_lines)
                                              or not example_lines[following]):
                    # 「羽」は空の読み行に語例があり、直後の「はね」に対応する。
                    pending_examples.extend(examples)
                    stats["deferred_example_lines"] += 1
                else:
                    readings[-1]["examples"].extend(examples)
                    stats["continued_example_lines"] += 1
            elif index < len(reading_lines) and not reading:
                stats["blank_reading_lines"] += 1
            stats["examples"] += len(examples)
        if not readings:
            raise ValueError(f"{number}行目: 読みがありません")
        if pending_examples:
            raise ValueError(f"{number}行目: 未対応の語例が残りました")

        # 備考欄は字単位の情報で、行内の音訓との対応が原典で明示されない。
        # 全文を各読みへ同じように保持し、特定の音訓への帰属は推測しない。
        note = "\n".join(part for part in lines(cells[3]) if part) or None
        if note:
            stats["notes"] += 1
        entries.append({"character": character, "old_forms": old_forms,
                        "label": label, "readings": readings, "note": note})

    entries.sort(key=lambda entry: ord(entry["character"]))
    return entries, stats


def cs(value: str | None) -> str:
    return "null" if value is None else json.dumps(value, ensure_ascii=True)


def generate(entries: list[dict]) -> str:
    out = [
        "// 自動生成ファイルです。手編集しないでください。",
        "// 収録データは文化庁『常用漢字表の音訓索引』を元に加工しています。",
        "// 出典・利用条件の詳細は LICENSE-NOTICES.md を参照してください。",
        "",
        "namespace KanjiVariants;",
        "",
        "internal static class GeneratedJoyoKanjiData",
        "{",
        "    internal static readonly int[] CodePoints = new int[]",
        "    {",
    ]
    for entry in entries:
        out.append(f"        0x{ord(entry['character']):X},")
    out.extend(["    };", "", "    internal static JoyoKanjiEntry[] Entries => EntryStore.Values;", "",
                "    private static class EntryStore", "    {",
                "        internal static readonly JoyoKanjiEntry[] Values = CreateEntries();",
                "    }", "",
                "    private static JoyoKanjiEntry[] CreateEntries() => new JoyoKanjiEntry[]", "    {"])
    for entry in entries:
        forms = entry["old_forms"]
        old = f"KanjiCharacter.FromScalar(0x{ord(forms[0]):X})" if len(forms) == 1 else "null"
        out.append(f"        new JoyoKanjiEntry(KanjiCharacter.FromScalar(0x{ord(entry['character']):X}), {old},")
        out.append("            Array.AsReadOnly(new KanjiReading[]")
        out.append("            {")
        for reading in entry["readings"]:
            examples = ", ".join(cs(example) for example in reading["examples"])
            example_expr = f"Array.AsReadOnly(new string[] {{ {examples} }})" if examples else "Array.Empty<string>()"
            out.append(f"                new(KanjiReadingType.{reading['kind']}, {cs(reading['reading'])}, {example_expr}, {cs(entry['note'])}),")
        out.append("            }))")
        out.append("        {")
        if forms:
            old_expr = ", ".join(f"KanjiCharacter.FromScalar(0x{ord(form):X})" for form in forms)
            out.append(f"            OldForms = Array.AsReadOnly(new KanjiCharacter[] {{ {old_expr} }}),")
        out.append(f"            SourceLabel = {cs(entry['label'])},")
        out.append("        },")
    out.extend(["    };", "}", ""])
    return "\n".join(out)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--check", action="store_true", help="既存の生成ファイルと一致するか確認")
    args = ap.parse_args()
    entries, stats = parse()
    generated = generate(entries)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf-8") != generated:
            raise SystemExit("生成ファイルが固定HTMLと一致しません")
    else:
        OUTPUT.write_text(generated, encoding="utf-8", newline="\n")
    print(f"source={SOURCE_URL}")
    print(f"entries={len(entries)} old_form_entries={stats['old_form_entries']} "
          f"On={stats['On']} Kun={stats['Kun']} examples={stats['examples']} "
          f"notes={stats['notes']} multiple_old_forms={stats['multiple_old_forms']} "
          f"bracketed_labels={stats['bracketed_labels']} "
          f"continued_example_lines={stats['continued_example_lines']} "
          f"deferred_example_lines={stats['deferred_example_lines']} "
          f"blank_reading_lines={stats['blank_reading_lines']}")


if __name__ == "__main__":
    main()
