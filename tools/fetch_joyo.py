# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""文化庁の原典HTMLを明示的に取得し、生成入力を更新する。"""

import hashlib
from pathlib import Path
import urllib.request


SOURCE_URL = "https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/kanji/joyokanjisakuin/index.html"
OUTPUT = Path(__file__).resolve().parent.parent / "data" / "Joyo" / "JoyoKanjiOnkunIndex.html"


def main() -> None:
    request = urllib.request.Request(SOURCE_URL, headers={"User-Agent": "KanjiVariants data updater"})
    with urllib.request.urlopen(request, timeout=30) as response:
        content = response.read()
    # 正本の公開HTMLはShift_JIS宣言。変更されていたら生成前に明示的に確認する。
    content.decode("cp932")
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_bytes(content)
    print(f"saved={OUTPUT} bytes={len(content)} sha256={hashlib.sha256(content).hexdigest()}")


if __name__ == "__main__":
    main()
