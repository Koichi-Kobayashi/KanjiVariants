# Copyright (c) 2026 Koichi Kobayashi
# Licensed under the MIT License.

"""JIS X 0212生成入力を固定URLから明示的に取得する（通常生成では実行しない）。"""

import io
import urllib.request
import zipfile

from generate_jis_x0212 import DATA, ICU_COMMIT, parse


def download(url):
    request = urllib.request.Request(url, headers={'User-Agent': 'KanjiVariants data updater'})
    with urllib.request.urlopen(request, timeout=45) as response:
        return response.read()


def main():
    (DATA / 'Unicode').mkdir(parents=True, exist_ok=True)
    (DATA / 'JIS').mkdir(parents=True, exist_ok=True)
    # Unihanの原典ファイルをヘッダーも含めて保存し、加工せず再配布します。
    archive = download('https://www.unicode.org/Public/18.0.0/ucd/Unihan.zip')
    with zipfile.ZipFile(io.BytesIO(archive)) as book:
        (DATA / 'Unicode/Unihan_OtherMappings.18.0.0.txt').write_bytes(book.read('Unihan_OtherMappings.txt'))
    base = f'https://raw.githubusercontent.com/unicode-org/icu/{ICU_COMMIT}/'
    (DATA / 'JIS/jisx-212.ucm').write_bytes(download(base + 'icu4c/source/data/mappings/jisx-212.ucm'))
    (DATA / 'JIS/ICU-LICENSE.txt').write_bytes(download(base + 'icu4c/LICENSE'))
    parse()
    print('固定URLからの取得と原典検証が完了しました')


if __name__ == '__main__':
    main()
