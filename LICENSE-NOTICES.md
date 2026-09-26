# KanjiVariants のライセンスとデータ出典

このパッケージには、ライセンス条件の異なるプログラムコードと文字データが含まれています。

## プログラムコード

KanjiVariants のプログラムコード（`GeneratedData.g.cs` に収録した外部データ由来の表を除く）は、Koichi Kobayashi が著作権を有し、MIT License の下で提供されます。

MIT License の本文は以下のとおりです。

このライセンス本文には、ソフトウェアを現状のまま提供すること、および作者・著作権者が請求や損害等について責任を負わないことが記載されています。README の免責事項も、本ライブラリおよび同梱データの利用による損害について作者は一切責任を負わない旨を示しています。

```text
MIT License

Copyright (c) 2026 Koichi Kobayashi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 同梱文字データ

`GeneratedData.g.cs` の表および `data/MJUniqueAlternatives.1.2.0.csv` には、以下の外部データに由来する内容が含まれます。これらのデータにはプログラムコードの MIT License ではなく、各出典元の条件が適用されます。

- MJ文字情報一覧表およびMJ縮退マップ: 独立行政法人情報処理推進機構（IPA）提供。CC BY-SA 2.1 JP。ライセンス本文と条件は <https://creativecommons.org/licenses/by-sa/2.1/jp/> を参照してください。改変・再構成したデータを再配布する場合は、同ライセンスの表示および継承条件に従ってください。
- Unicode IVD および Unicode Standardized Variants: Unicode, Inc. 提供。Unicode の利用条件 <https://www.unicode.org/terms_of_use.html> に従ってください。
- JIS X 0208 対応表: Python の `euc_jp` デコーダーによる区点走査から生成しています。Python の文字コード実装および Unicode データに関する条件は、使用した Python ディストリビューションのライセンス・通知も確認してください。

各データのバージョン、生成元および詳細は同梱の `README.md` を参照してください。
