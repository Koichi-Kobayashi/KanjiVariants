# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""文部科学省の平成29年告示版学年別漢字配当表を明示的に取得する。"""

import hashlib
from pathlib import Path
import urllib.request


SOURCE_URL = "https://www.mext.go.jp/content/20230120-mxt_kyoiku02-100002604_01.pdf"
OUTPUT = Path(__file__).resolve().parent.parent / "data" / "EducationKanjiGradeTable.pdf"


def main() -> None:
    request = urllib.request.Request(SOURCE_URL, headers={"User-Agent": "KanjiVariants data updater"})
    with urllib.request.urlopen(request, timeout=60) as response:
        content = response.read()
    if not content.startswith(b"%PDF-"):
        raise ValueError("PDFではない応答です")
    OUTPUT.write_bytes(content)
    print(f"saved={OUTPUT} bytes={len(content)} sha256={hashlib.sha256(content).hexdigest()}")
    print("固定の転記ファイルとPDF画像を照合してから、Generatorを実行してください。")


if __name__ == "__main__":
    main()
