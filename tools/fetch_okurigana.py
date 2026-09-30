# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""文化庁「送り仮名の付け方」の公式HTMLを明示的に更新する。"""

import hashlib
from pathlib import Path
import urllib.request


BASE_URL = "https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/okurikana/"
OUTPUT = Path(__file__).resolve().parent.parent / "data" / "Okurigana"
SOURCES = {f"rule{number}.html": f"honbun{number:02}.html" for number in range(1, 8)}
SOURCES["appendix.html"] = "huhyo.html"


def main() -> None:
    contents = {}
    for filename, page in SOURCES.items():
        request = urllib.request.Request(BASE_URL + page, headers={"User-Agent": "KanjiVariants data updater"})
        with urllib.request.urlopen(request, timeout=30) as response:
            content = response.read()
        text = content.decode("cp932")
        title = "付表の語" if filename == "appendix.html" else "通則" + filename[4]
        if "charset=shift_jis" not in text.lower() or "送り仮名の付け方" not in text or title not in text:
            raise ValueError(f"{filename}: 文字コードまたは本文の見出しが想定と異なります")
        contents[filename] = content

    # 全ページを検証してから固定入力を更新します。通常の生成処理からは呼びません。
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for filename, content in contents.items():
        (OUTPUT / filename).write_bytes(content)
        print(f"{filename}: url={BASE_URL + SOURCES[filename]} bytes={len(content)} sha256={hashlib.sha256(content).hexdigest()}")


if __name__ == "__main__":
    main()
