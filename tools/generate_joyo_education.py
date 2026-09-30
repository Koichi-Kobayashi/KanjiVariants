# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定した文部科学省PDFから常用漢字の音訓別学校段階を生成する。

実行には pdfplumber が必要。OCRは使用せず、PDFの文字と座標だけを読む。
"""

from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import unicodedata

import pdfplumber

from generate_joyo import OUTPUT as JOYO_OUTPUT, generate as generate_joyo, parse as parse_joyo
from generate_education_kanji import (OUTPUT as EDUCATION_OUTPUT,
                                       generate as generate_education,
                                       parse as parse_education)


ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "data" / "JoyoKanjiSchoolStages2017.pdf"
OUTPUT = ROOT / "KanjiVariants" / "Generated" / "GeneratedJoyoKanjiEducationData.g.cs"
SOURCE_URL = "https://www.mext.go.jp/a_menu/shotou/new-cs/__icsFiles/afieldfile/2017/05/15/1385768.pdf"
SOURCE_SHA256 = "0bc189982a50122f1e35dd6a751bfdff0ec5a0ad67e99eda916d012f859a5ae4"
MAIN_BASES = (70.6, 310.8, 551.0)
APPENDIX_BASES = (68.0, 305.6, 543.2)
EXPECTED_MAIN_STAGES = Counter({0: 2062, 1: 2008, 2: 318})
EXPECTED_APPENDIX_STAGES = Counter({0: 33, 1: 53, 2: 30})


def normalize(value: str) -> str:
    value = unicodedata.normalize("NFC", value)
    return "".join(chr(ord(c) - 0x60) if "\u30a1" <= c <= "\u30f6" else c
                   for c in value)


def rows(page, bases: tuple[float, ...], min_top: float, max_top: float):
    """三つの並列欄を、欄順・上から下の順に行へ戻す。"""
    words = page.extract_words(x_tolerance=1, y_tolerance=2)
    for panel, base in enumerate(bases):
        selected = [w for w in words
                    if base - 2 <= w["x0"] < base + 220
                    and min_top <= w["top"] <= max_top]
        groups: list[list[dict]] = []
        for word in sorted(selected, key=lambda w: (w["top"], w["x0"])):
            if not groups or abs(groups[-1][0]["top"] - word["top"]) > 2:
                groups.append([word])
            else:
                groups[-1].append(word)
        for group in groups:
            slots: list[list[dict]] = [[] for _ in range(6)]
            for word in group:
                offset = word["x0"] - base
                if offset < 20:
                    slot = 0
                elif offset < 43:
                    slot = 1
                elif offset < 105:
                    slot = 2
                elif offset < 142:
                    slot = 3
                elif offset < 182:
                    slot = 4
                else:
                    slot = 5
                slots[slot].append(word)
            fields = ["".join(w["text"] for w in sorted(slot, key=lambda w: w["x0"]))
                      for slot in slots]
            yield panel, round(group[0]["top"], 1), fields, slots


def stage_of(fields: list[str], where: str) -> int:
    marks = fields[3:]
    if sum(mark == "○" for mark in marks) != 1 or any(mark and mark != "○" for mark in marks):
        raise ValueError(f"{where}: 学校段階の○を一意に判定できません: {marks!r}")
    return marks.index("○")


def parse_main(pdf, joyo: list[dict]) -> list[dict]:
    records: list[dict] = []
    headers: list[str] = []
    current: str | None = None
    for page_index in range(1, 50):
        for panel, top, fields, slots in rows(pdf.pages[page_index], MAIN_BASES, 100, 540):
            character, grade, reading = fields[:3]
            where = f"PDF {page_index + 1}ページ・欄{panel + 1}・y={top}"
            if character:
                if len(character) != 1:
                    raise ValueError(f"{where}: 漢字欄が一文字ではありません: {character!r}")
                headers.append(character)
                current = character
            if not current or not reading or not slots[2]:
                raise ValueError(f"{where}: 音訓または対応する漢字がありません")
            reading_x = min(w["x0"] for w in slots[2])
            offset = reading_x - MAIN_BASES[panel]
            if not (49.5 <= offset <= 51.5 or 55.5 <= offset <= 58.0):
                raise ValueError(f"{where}: 音訓欄の字下げ位置が想定外です: {offset:.1f}")
            if grade and not (character and grade in "123456" and len(grade) == 1):
                raise ValueError(f"{where}: 配当学年の位置または値が想定外です: {grade!r}")
            records.append({
                "character": current, "header": character, "grade": grade,
                "reading": reading, "stage": stage_of(fields, where),
                "special": offset > 54, "page": page_index + 1,
                "panel": panel, "top": top,
            })

    joyo_by_char = {entry["character"]: entry for entry in joyo}
    # 原PDFの𠮟だけは文字のToUnicode対応が欠落している。ページ・行・
    # 直前の字・二つの音訓・既存本表をすべて確認できた場合だけ補正する。
    exception = [i for i, record in enumerate(records)
                 if record["page"] == 20 and record["panel"] == 2
                 and record["top"] == 184.8]
    if (len(exception) != 1 or "𠮟" in headers
            or [record["reading"] for record in records[exception[0]:exception[0] + 2]]
               != ["シツ", "しかる"]
            or any(record["character"] != "七" or record["header"]
                   for record in records[exception[0]:exception[0] + 2])
            or [item["reading"] for item in joyo_by_char["𠮟"]["readings"]]
               != ["シツ", "しかる"]):
        raise ValueError("𠮟のPDF文字層欠落が既知の状態と異なります。原典を再確認してください")
    for record in records[exception[0]:exception[0] + 2]:
        record["character"] = "𠮟"
    headers.insert(headers.index("七") + 1, "𠮟")

    if len(headers) != 2136 or len(set(headers)) != 2136 or set(headers) != set(joyo_by_char):
        raise ValueError("本表の漢字2136字が既存JoyoKanjiと一致しません")
    if len(records) != 4388:
        raise ValueError(f"本表の音訓件数が不正です: {len(records)}")
    if Counter(record["stage"] for record in records) != EXPECTED_MAIN_STAGES:
        raise ValueError("本表の学校段階別件数が不正です")

    kinds = Counter()
    pdf_readings: dict[tuple[str, str], dict] = {}
    for record in records:
        reading = record["reading"]
        if re.search(r"[ァ-ヺ]", reading):
            kind = "On"
        elif re.search(r"[ぁ-ゖ]", reading):
            kind = "Kun"
        else:
            raise ValueError(f"音訓の種別を判定できません: {record!r}")
        kinds[kind] += 1
        key = (record["character"], normalize(reading))
        if key in pdf_readings:
            raise ValueError(f"PDF本表で漢字・音訓が重複しています: {key!r}")
        pdf_readings[key] = record
        record["kind"] = kind
    if kinds != Counter({"On": 2352, "Kun": 2036}):
        raise ValueError(f"音読み・訓読みの件数が不正です: {kinds}")

    joyo_readings: dict[tuple[str, str], dict] = {}
    for entry in joyo:
        for reading in entry["readings"]:
            key = (entry["character"], normalize(reading["reading"]))
            if key in joyo_readings:
                raise ValueError(f"既存JoyoKanjiに正規化後の重複音訓があります: {key!r}")
            joyo_readings[key] = reading
    if len(joyo_readings) != 4388 or set(pdf_readings) != set(joyo_readings):
        missing = sorted(set(joyo_readings) - set(pdf_readings))
        extra = sorted(set(pdf_readings) - set(joyo_readings))
        raise ValueError(f"既存JoyoKanjiとの音訓照合に失敗: missing={missing!r} extra={extra!r}")
    for key, record in pdf_readings.items():
        if record["kind"] != joyo_readings[key]["kind"]:
            raise ValueError(f"音訓型が既存JoyoKanjiと異なります: {key!r}")

    pdf_grades = {record["character"]: int(record["grade"])
                  for record in records if record["grade"]}
    education = {character: grade
                 for grade, characters in parse_education().items()
                 for character in characters}
    if len(pdf_grades) != 1026 or pdf_grades != education:
        raise ValueError("小学校配当学年が既存EducationKanjiと一致しません")
    return records


def parse_appendix(pdf) -> list[dict]:
    first: list[dict] = []
    for page_index in (50, 51):
        bases = APPENDIX_BASES if page_index == 50 else APPENDIX_BASES[:2]
        min_top = 125 if page_index == 50 else 112
        for panel, top, fields, _ in rows(pdf.pages[page_index], bases, min_top, 545):
            if page_index == 51 and panel == 1 and top > 160:
                continue
            word, _, reading = fields[:3]
            where = f"PDF {page_index + 1}ページ・付表1欄{panel + 1}・y={top}"
            if reading and "○" in fields[3:]:
                if not word:
                    raise ValueError(f"{where}: 語がありません")
                first.append({"words": [word], "raw_reading": reading,
                              "stage": stage_of(fields, where),
                              "kind": "SpecialReading"})
            elif word and not reading and not any(fields[3:]):
                if not first:
                    raise ValueError(f"{where}: 異表記の親行がありません")
                first[-1]["words"].append(word)
            elif not word and reading and not any(fields[3:]):
                if not first:
                    raise ValueError(f"{where}: 注記の親行がありません")
                first[-1]["raw_reading"] += reading
            else:
                raise ValueError(f"{where}: 未解釈の付表行: {fields!r}")
    for entry in first:
        raw = entry.pop("raw_reading")
        match = re.fullmatch(r"([^（）]+)(?:（([^（）]+)）)?", raw)
        if not match:
            raise ValueError(f"付表1の音訓・注記を分離できません: {raw!r}")
        entry["reading"] = match.group(1)
        # 注記は外側の括弧も含めて原典の表記を保持します。
        entry["note"] = f"（{match.group(2)}）" if match.group(2) else None
    if (len(first) != 116 or sum(len(e["words"]) for e in first) != 123
            or Counter(e["stage"] for e in first) != EXPECTED_APPENDIX_STAGES):
        raise ValueError("付表1の件数または学校段階分布が不正です")
    if next(e for e in first if e["words"][0] == "師走")["note"] != "（「しはす」とも言う。）":
        raise ValueError("師走の原典注記を保持できません")

    second: list[dict] = []
    for panel, top, fields, _ in rows(pdf.pages[51], APPENDIX_BASES[1:2], 218, 365):
        word, _, reading = fields[:3]
        where = f"PDF 52ページ・付表2・y={top}"
        if not word or not reading:
            raise ValueError(f"{where}: 語または読みがありません")
        second.append({"words": [word], "reading": reading,
                       "stage": stage_of(fields, where),
                       "kind": "PrefectureName", "note": None})
    if len(second) != 12 or any(e["stage"] != 0 for e in second):
        raise ValueError("付表2の件数または学校段階が不正です")
    all_words = [word for entry in first + second for word in entry["words"]]
    if len(all_words) != len(set(all_words)):
        raise ValueError("付表内の語が重複しています")
    return first + second


def cs(value: str | None) -> str:
    return "null" if value is None else json.dumps(value, ensure_ascii=True)


def generate(main: list[dict], appendix: list[dict], joyo: list[dict]) -> str:
    by_key = {(record["character"], normalize(record["reading"])): record for record in main}
    offsets = [0]
    stages: list[int] = []
    special: list[int] = []
    for entry in joyo:
        for reading in entry["readings"]:
            record = by_key[(entry["character"], normalize(reading["reading"]))]
            stages.append(record["stage"])
            special.append(int(record["special"]))
        offsets.append(len(stages))
    if len(offsets) != 2137 or offsets[-1] != 4388:
        raise ValueError("生成データのオフセット数が不正です")
    out = [
        "// 自動生成ファイルです。手編集しないでください。",
        "// 収録データは文部科学省『音訓の小・中・高等学校段階別割り振り表』を元に加工しています。",
        "// 出典・利用条件の詳細は LICENSE-NOTICES.md を参照してください。",
        "",
        "namespace KanjiVariants;",
        "",
        "internal static class GeneratedJoyoKanjiEducationData",
        "{",
    ]
    for name, values, cs_type, width in (
        ("Offsets", offsets, "ushort", 20),
        ("Stages", stages, "byte", 40),
        ("Special", special, "byte", 40),
    ):
        out.append(f"    internal static readonly {cs_type}[] {name} = new {cs_type}[]")
        out.append("    {")
        for start in range(0, len(values), width):
            out.append("        " + ", ".join(map(str, values[start:start + width])) + ",")
        out.extend(["    };", ""])
    out.extend([
        "    internal static readonly JoyoKanjiAppendixEntry[] Appendix = new JoyoKanjiAppendixEntry[]",
        "    {",
    ])
    stage_names = ("Elementary", "JuniorHigh", "HighSchool")
    for entry in appendix:
        words = ", ".join(cs(word) for word in entry["words"])
        out.append(
            f"        new(Array.AsReadOnly(new string[] {{ {words} }}), "
            f"{cs(entry['reading'])}, SchoolStage.{stage_names[entry['stage']]}, "
            f"JoyoKanjiAppendixKind.{entry['kind']}, {cs(entry['note'])}),"
        )
    out.extend(["    };", "}", ""])
    return "\n".join(out)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="生成結果と既存ファイルの一致を確認")
    args = parser.parse_args()
    if hashlib.sha256(SOURCE.read_bytes()).hexdigest() != SOURCE_SHA256:
        raise ValueError("原典PDFのSHA-256が固定版と異なります。PDFと抽出結果を再確認してください")
    joyo, _ = parse_joyo()
    if JOYO_OUTPUT.read_text(encoding="utf-8") != generate_joyo(joyo):
        raise ValueError("既存JoyoKanjiの生成済みデータが固定HTMLと一致しません")
    education = parse_education()
    if EDUCATION_OUTPUT.read_text(encoding="utf-8") != generate_education(education):
        raise ValueError("既存EducationKanjiの生成済みデータが固定配当表と一致しません")
    with pdfplumber.open(SOURCE) as pdf:
        if len(pdf.pages) != 52:
            raise ValueError(f"原典PDFのページ数が想定外です: {len(pdf.pages)}")
        readings = parse_main(pdf, joyo)
        appendix = parse_appendix(pdf)
    generated = generate(readings, appendix, joyo)
    if args.check:
        if not OUTPUT.exists() or OUTPUT.read_text(encoding="utf-8") != generated:
            raise SystemExit("生成ファイルが固定の文部科学省PDFと一致しません")
    else:
        OUTPUT.write_text(generated, encoding="utf-8", newline="\n")
    print(f"source={SOURCE_URL}")
    print(f"kanji=2136 readings={len(readings)} special={sum(r['special'] for r in readings)}")
    print(f"stages={dict(Counter(r['stage'] for r in readings))}")
    print(f"appendix1=116 words=123 appendix2=12 check={args.check}")


if __name__ == "__main__":
    main()
