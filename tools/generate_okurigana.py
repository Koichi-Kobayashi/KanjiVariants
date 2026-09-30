# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""固定した文化庁公式HTMLの掲載語を解析してC#検索データを生成する。"""

from __future__ import annotations

import argparse
from collections import Counter
from dataclasses import dataclass, field, replace
import hashlib
from html.parser import HTMLParser
import json
from pathlib import Path
import re

from fetch_okurigana import BASE_URL, SOURCES


ROOT = Path(__file__).resolve().parent.parent
SOURCE = ROOT / "data" / "Okurigana"
OUTPUT = ROOT / "KanjiVariants" / "Generated" / "GeneratedOkuriganaData.g.cs"
HASHES = {
    "rule1.html": "ed8c2e317a386c95f79a70f0b0c2d1d250c466902be30b3808caac4aa6634484",
    "rule2.html": "d72246784687c47c22e08dfdacb6f24b8e4735343b7291514af12b25388cba85",
    "rule3.html": "7b66cdf9cda567a244bdc0166c8e3f26918cdc4dbf1af60a66af33cf562f6517",
    "rule4.html": "3d3c85b1b16da7eca14c64dfc1e9cc46953830165a6847b67c508f1f0ee9a202",
    "rule5.html": "5acd07e62258504b31ceaba45536501f1a4f68464185be5ddb125d2589fa8f9d",
    "rule6.html": "7aacb986c10ccb8141849952d142c8cf5e6abd3a1432c96f9cf36361c133385a",
    "rule7.html": "f919ea6ec580387cc602a3053cf7db3436daa5e0162a339882fa6b84b91d4af5",
    "appendix.html": "c76e5612ba2c43cd6c0ff20e33b3b0590b4485ab7c1b0fd034f2ab9e186962b7",
}
BLOCK_COUNTS = {1: 5, 2: 5, 3: 3, 4: 5, 5: 4, 6: 3, 7: 3, None: 2}
ENTRY_COUNTS = {1: 71, 2: 67, 3: 30, 4: 73, 5: 29, 6: 112, 7: 86, None: 15}
KIND_COUNTS = {"Principle": 208, "Exception": 200, "Permitted": 60, "Appendix": 15}
KINDS = {"本則": "Principle", "例外": "Exception", "許容": "Permitted"}


@dataclass
class Node:
    tag: str
    attrs: dict[str, str] = field(default_factory=dict)
    children: list[Node | str] = field(default_factory=list)

    def text(self) -> str:
        if self.tag == "br":
            return "\n"
        text = "".join(child.text() if isinstance(child, Node) else child for child in self.children)
        return "\n" + text + "\n" if self.tag == "p" else text

    def find(self, predicate) -> list[Node]:
        result = [self] if predicate(self) else []
        for child in self.children:
            if isinstance(child, Node):
                result.extend(child.find(predicate))
        return result


class Document(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.root = Node("root")
        self.stack = [self.root]

    def handle_starttag(self, tag, attrs):
        node = Node(tag, dict(attrs))
        self.stack[-1].children.append(node)
        if tag not in {"br", "img", "hr", "meta", "link", "input"}:
            self.stack.append(node)

    def handle_startendtag(self, tag, attrs):
        self.handle_starttag(tag, attrs)
        if self.stack[-1].tag == tag:
            self.stack.pop()

    def handle_endtag(self, tag):
        # 固定原典には通則6の孤立した </u> があります。本文の階層は閉じません。
        for index in range(len(self.stack) - 1, 0, -1):
            if self.stack[index].tag == tag:
                del self.stack[index:]
                break

    def handle_data(self, data):
        self.stack[-1].children.append(data)


@dataclass(frozen=True)
class Entry:
    word: str
    rule: int | None
    kind: str
    alternatives: tuple[str, ...] = ()
    note: str | None = None


def compact(value: str) -> str:
    return re.sub(r"\s+", " ", value).strip()


def combine(*notes: str | None) -> str | None:
    return "\n".join(dict.fromkeys(note for note in notes if note)) or None


def terms(text: str, rule: int | None, kind: str, note: str | None,
          alternative_brackets: bool = False) -> list[Entry]:
    text = text.replace("〔例〕", "").strip()
    # 通則4の「向かい (向い)」は、空白を含む同一項目として扱います。
    text = re.sub(r"\s+([（(〔])", r"\1", text)
    entries = []
    for token in text.split():
        match = re.fullmatch(r"([^（(〔]+)(?:([（(〔])([^）)〕]+)([）)〕]))?", token)
        if not match:
            raise ValueError(f"語例の括弧構造を解釈できません: {token!r}")
        word, opening, inside, closing = match.groups()
        local_note = note
        alternatives = ()
        if inside:
            if alternative_brackets:
                alternatives = tuple(inside.split("・"))
            elif opening == "〔":
                # 構成関係です。原典の閉じ括弧が ')' の箇所もそのまま注記に残します。
                local_note = combine(local_note, f"原典の構成関係表記: {opening}{inside}{closing}")
            else:
                local_note = combine(local_note, f"原典の読み注記: {opening}{inside}{closing}")
        if "《" in word or "》" in word:
            if not re.fullmatch(r"(?:[^《》]*《[^《》]+》[^《》]*)", word):
                raise ValueError(f"通則7の区分表記が不正です: {word}")
            local_note = combine(local_note, f"原典表記: {word}")
            word = word.replace("《", "").replace("》", "")
        for single_word in word.split("・"):
            if not single_word or any(c in single_word for c in "。，,「」〔〕()（）"):
                raise ValueError(f"語以外の文字列を取得しています: {single_word!r}")
            entries.append(Entry(single_word, rule, kind, alternatives, local_note))
    if not entries:
        raise ValueError("空の語例ブロックです")
    return entries


def parse_page(filename: str, rule: int | None) -> list[Entry]:
    path = SOURCE / filename
    if not path.is_file():
        raise ValueError(f"公式HTMLがありません: {path}")
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != HASHES[filename]:
        raise ValueError(f"{filename}: 固定HTMLと不一致です。原典の構造と語例を再確認してください")
    doc = Document()
    doc.feed(raw.decode("cp932"))
    headings = doc.root.find(lambda node: node.tag == "h3")
    expected_title = "付表の語" if rule is None else f"通則{rule}"
    if len(headings) != 1 or not compact(headings[0].text()).endswith(expected_title):
        raise ValueError(f"{filename}: 通則番号または付表見出しが不正です")
    containers = doc.root.find(lambda node: node.attrs.get("class") == "joho_main")
    if len(containers) != 1:
        raise ValueError(f"{filename}: 本文領域が一意ではありません")
    main = containers[0]
    blocks = main.find(lambda node: node.attrs.get("class") == "read_text")
    if len(blocks) != BLOCK_COUNTS[rule]:
        raise ValueError(f"{filename}: 語例ブロック数が想定と異なります")
    seen: set[int] = set()
    entries: list[Entry] = []
    # 通則7は公式「本文の見方及び使い方」に従い、例外として区分し番号7を保ちます。
    # https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/okurikana/mikata.html
    kind = "Appendix" if rule is None else "Exception" if rule == 7 else None
    context = None
    caution = False
    seen_headings = []
    for node in main.children:
        if not isinstance(node, Node):
            continue
        text = compact(node.text())
        if node.tag == "h4":
            seen_headings.append(text)
            if text in KINDS:
                kind, context, caution = KINDS[text], None, False
            elif text == "（注意）":
                # 注意中の語例は本則との関係を示す情報です。注記を付して収録します。
                kind, context, caution = "Principle", None, True
            else:
                raise ValueError(f"{filename}: 未知の種別見出し: {text}")
        elif node.tag in {"p", "h5"}:
            context = text
            if caution and rule == 1:
                quoted = re.findall(r"「([^「」]+)」", text)
                if quoted != ["着る", "寝る", "来る"]:
                    raise ValueError("通則1の注意語例が変わっています")
                entries.extend(terms(" ".join(quoted), rule, "Principle", text))
            elif caution and rule == 6:
                quoted = re.findall(r"「([^「」]+)」", text)
                if len(quoted) != 4:
                    raise ValueError("通則6の注意語例が変わっています")
                entries.extend(terms(" ".join(quoted), rule, "Principle", text, True))
        elif node.attrs.get("class") == "read_text":
            if id(node) in seen or kind is None:
                raise ValueError(f"{filename}: 語例の二重取得または種別未指定")
            seen.add(id(node))
            raw_text = node.text()
            if rule == 4 and text.startswith("（注意）"):
                # 語として列挙されたものではなく用法の説明です。例外の全語へ付記します。
                entries = [replace(entry, note=combine(entry.note, text))
                           if entry.kind == "Exception" else entry for entry in entries]
                continue
            if rule == 7 and text.startswith("(1) 「"):
                entries = [replace(entry, note=combine(entry.note, text)) for entry in entries]
                continue
            if rule == 7:
                # ア・イ・ウは見出しです。《》は具体的な掲載語の一部として注記に保存します。
                raw_text = "\n".join(line for line in raw_text.splitlines()
                                     if not re.match(r"^\s*[アイウ]\s", line))
            if rule is None and node.find(lambda child: child.tag == "p"):
                # 付表1では6語の後に、同じ2語の許容表記が同一HTML要素に示されます。
                paragraphs = node.find(lambda child: child.tag == "p")
                if len(paragraphs) != 1:
                    raise ValueError("付表1の許容説明が一意ではありません")
                marker = paragraphs[0].text()
                before, after = raw_text.split(marker)
                first = terms(before, None, "Appendix", context)
                permitted = terms(after, None, "Appendix", compact(marker), True)
                lookup = {entry.word: entry for entry in permitted}
                if len(lookup) != 2 or not set(lookup) <= {entry.word for entry in first}:
                    raise ValueError("付表1の許容語が元の掲載語と対応しません")
                entries.extend(replace(entry, alternatives=lookup[entry.word].alternatives,
                                       note=combine(entry.note, lookup[entry.word].note))
                               if entry.word in lookup else entry for entry in first)
            else:
                entries.extend(terms(raw_text, rule, kind, context, kind == "Permitted"))
    if seen != {id(node) for node in blocks}:
        raise ValueError(f"{filename}: 未処理の語例ブロックがあります")
    expected_headings = {
        1: ["本則", "例外", "許容", "（注意）"],
        2: ["本則", "許容", "（注意）"],
        3: ["本則", "例外"], 4: ["本則", "例外", "許容"],
        5: ["本則", "例外"], 6: ["本則", "許容", "（注意）"], 7: [], None: [],
    }
    if seen_headings != expected_headings[rule] or not entries:
        raise ValueError(f"{filename}: 種別構成または語例件数が不正です")
    return entries


def parse() -> list[Entry]:
    entries = [entry for rule in range(1, 8) for entry in parse_page(f"rule{rule}.html", rule)]
    entries.extend(parse_page("appendix.html", None))
    if Counter(entry.rule for entry in entries) != ENTRY_COUNTS or Counter(entry.kind for entry in entries) != KIND_COUNTS:
        raise ValueError("固定原典の通則別または種別の掲載件数と一致しません")
    if len(entries) != len(set(entries)):
        raise ValueError("完全重複したEntryがあります")
    for entry in entries:
        if not entry.word or any(not form for form in entry.alternatives):
            raise ValueError("空のWordまたはAlternativeFormsがあります")
        if (entry.rule is None) != (entry.kind == "Appendix") or (entry.rule is not None and not 1 <= entry.rule <= 7):
            raise ValueError("通則番号と種別が不整合です")
        if len(entry.alternatives) != len(set(entry.alternatives)) or entry.word in entry.alternatives:
            raise ValueError("代替表記が重複しています")
    return entries


def generate(entries: list[Entry]) -> str:
    quote = lambda value: "null" if value is None else json.dumps(value, ensure_ascii=False)
    lines = [
        "// 自動生成ファイルです。手編集しないでください。",
        "// 収録データは文化庁『送り仮名の付け方』の公式HTMLを元に加工しています。",
        "// 出典・利用条件の詳細は LICENSE-NOTICES.md を参照してください。",
        "", "#nullable enable", "", "namespace KanjiVariants;", "", "internal static class GeneratedOkuriganaData", "{",
        "    // 原典の通則・種別・語例の掲載順を保持します。",
        "    internal static readonly OkuriganaEntry[] Entries = new OkuriganaEntry[]", "    {",
    ]
    for entry in entries:
        args = [quote(entry.word), str(entry.rule) if entry.rule else "null",
                "OkuriganaRuleKind." + entry.kind, quote(entry.note)]
        args.extend(quote(form) for form in entry.alternatives)
        lines.append("        Entry(" + ", ".join(args) + "),")
    lines.extend(["    };", "", "    private static OkuriganaEntry Entry(string word, int? rule, OkuriganaRuleKind kind,",
                  "        string? note, params string[] forms) => new(word, rule, kind, Array.AsReadOnly(forms), note);",
                  "}", ""])
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="生成結果と既存ファイルの一致を確認")
    args = parser.parse_args()
    entries = parse()
    generated = generate(entries)
    if args.check:
        if not OUTPUT.is_file() or OUTPUT.read_text(encoding="utf-8") != generated:
            raise SystemExit("生成済みの送り仮名データが固定HTMLと一致しません")
    else:
        OUTPUT.parent.mkdir(parents=True, exist_ok=True)
        OUTPUT.write_text(generated, encoding="utf-8", newline="\n")
    print(f"entries={len(entries)} rules={dict(Counter(entry.rule for entry in entries))}")
    print(f"kinds={dict(Counter(entry.kind for entry in entries))}")
    print(f"alternative_entries={sum(bool(entry.alternatives) for entry in entries)} alternative_forms={sum(len(entry.alternatives) for entry in entries)}")
    for filename, page in SOURCES.items():
        print(f"{filename}: {BASE_URL + page}")


if __name__ == "__main__":
    main()
