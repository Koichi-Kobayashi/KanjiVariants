# KanjiVariants のライセンスとデータ出典

このパッケージには、ライセンス条件の異なるプログラムコードと文字データが含まれています。

## プログラムコード

KanjiVariants のプログラムコード（`GeneratedData.g.cs`、`GeneratedJoyoKanjiData.g.cs`、`GeneratedEducationKanjiData.g.cs`、`GeneratedJoyoKanjiEducationData.g.cs`、`GeneratedJinmeiyoKanjiData.g.cs`、`GeneratedMjCharacterData.g.cs`、`GeneratedJisX0212Data.g.cs`、`GeneratedOkuriganaData.g.cs` に収録した外部データ由来の表を除く）は、Koichi Kobayashi が著作権を有し、MIT License の下で提供されます。

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

## 文字データ

`GeneratedData.g.cs`、`GeneratedJinmeiyoKanjiData.g.cs`、`GeneratedMjCharacterData.g.cs`、`GeneratedJisX0212Data.g.cs` の表およびリポジトリで公開している `data/MJUniqueAlternatives.1.2.0.csv` には、以下の外部データに由来する内容が含まれます。これらのデータにはプログラムコードの MIT License ではなく、各出典元の条件が適用されます。

- MJ文字情報一覧表およびMJ縮退マップ: 独立行政法人情報処理推進機構（IPA）提供。CC BY-SA 2.1 JP。ライセンス本文と条件は <https://creativecommons.org/licenses/by-sa/2.1/jp/> を参照してください。改変・再構成したデータを再配布する場合は、同ライセンスの表示および継承条件に従ってください。
- Unicode IVD、Unicode Standardized Variants および Unihan: Unicode, Inc. 提供。Unicode の利用条件 <https://www.unicode.org/terms_of_use.html> に従ってください。
- JIS X 0208 対応表: Python の `euc_jp` デコーダーによる区点走査から生成しています。Python の文字コード実装および Unicode データに関する条件は、使用した Python ディストリビューションのライセンス・通知も確認してください。

各データのバージョン、生成元および詳細は同梱の [README.md](README.md) を参照してください。

`data/JoyoKanjiOnkunIndex.html` と `GeneratedJoyoKanjiData.g.cs` の文字・音訓・語例・備考は、[文化庁「常用漢字表の音訓索引」](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/kanji/joyokanjisakuin/index.html)をもとに、KanjiVariants用に加工して作成しています。原典HTMLを固定保存し、表の行・音訓・語例を抽出、空行や改行による続き行を整理して、KanjiVariants用の検索表に加工しています。備考は原文の字単位の情報として保持します。文化庁サイトに適用される[文部科学省ウェブサイト利用規約](https://www.mext.go.jp/b_menu/1351168.htm)を参照してください。この外部公開データ由来の表を、独自プログラムコードのMIT Licenseと同一視しません。

`data/EducationKanjiGradeTable.pdf`、その転記 `data/EducationKanjiGradeTable.txt`、`GeneratedEducationKanjiData.g.cs` の文字と学年は、[文部科学省「小学校学習指導要領（平成29年告示）」別表「学年別漢字配当表」](https://www.mext.go.jp/content/20230120-mxt_kyoiku02-100002604_01.pdf)をもとに、KanjiVariants用に加工して作成しています。画像の表を転記し、掲載順を保持した学年別一覧とUnicode順の検索表に加工しています。[文部科学省ウェブサイト利用規約](https://www.mext.go.jp/b_menu/1351168.htm)を参照してください。この外部公開データ由来の表を、独自プログラムコードのMIT Licenseと同一視しません。

`data/JoyoKanjiSchoolStages2017.pdf` と `GeneratedJoyoKanjiEducationData.g.cs` の音訓別学校段階、1字下げ情報、付表1・付表2の語は、[文部科学省「音訓の小・中・高等学校段階別割り振り表（平成29年3月）」](https://www.mext.go.jp/a_menu/shotou/new-cs/1385768.htm)の[原典PDF](https://www.mext.go.jp/a_menu/shotou/new-cs/__icsFiles/afieldfile/2017/05/15/1385768.pdf)をもとに、KanjiVariants用に加工して作成しています。固定PDFの文字と座標を抽出し、既存の常用漢字・学年データと照合して検索表へ再構成しています。[文部科学省ウェブサイト利用規約](https://www.mext.go.jp/b_menu/1351168.htm)を参照してください。この外部公開データ由来の表を、独自プログラムコードのMIT Licenseと同一視しません。

`data/Okurigana/` の公式HTMLと `GeneratedOkuriganaData.g.cs` の掲載語・許容表記・注記は、[文化庁「送り仮名の付け方」](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/okurikana/index.html)をもとに、KanjiVariants用に加工・再構成しています。通則1～7と付表のHTMLを固定保存し、本文の見出しと語例を解析して本則・例外・許容・付表別の検索データへ整理しています。読み・構成関係・《　》の原典表記等は注記に分離しています。文化庁サイトに適用される[文部科学省ウェブサイト利用規約](https://www.mext.go.jp/b_menu/1351168.htm)を参照してください。この外部公開データ由来の表を、独自プログラムコードのMIT Licenseと同一視しません。

## JIS X 0212対応表

`data/jisx-212.ucm` と `GeneratedJisX0212Data.g.cs` は、Unicode ICUのJIS X 0212対応表（コミット `61607c27732906d36c5bd4d23ecc092f89f53a2b`）に由来します。所属bit表に加工しています。漢字部分の照合にはUnihan 18.0.0の `data/Unihan_OtherMappings.18.0.0.txt` を使用しています。独自コードのMIT Licenseとは同一視しません。取得元と仕様はREADMEを参照してください。Unicodeの利用条件は上記のUnicode記載に従い、ICU表に適用される著作権表示と許諾本文を以下に保持します。固定版ICUのライセンス全文は `data/ICU-LICENSE.txt` に保存しています。

```text
ICU License - ICU 1.8.1 and later

COPYRIGHT AND PERMISSION NOTICE

Copyright (C) 2016 and later: Unicode, Inc. and others. License & terms of use: http://www.unicode.org/copyright.html
Copyright (c) 1995-2016 International Business Machines Corporation and others

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining
a copy of this software and associated documentation files (the
"Software"), to deal in the Software without restriction, including
without limitation the rights to use, copy, modify, merge, publish,
distribute, and/or sell copies of the Software, and to permit persons
to whom the Software is furnished to do so, provided that the above
copyright notice(s) and this permission notice appear in all copies of
the Software and that both the above copyright notice(s) and this
permission notice appear in supporting documentation.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF THIRD PARTY RIGHTS. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
HOLDERS INCLUDED IN THIS NOTICE BE LIABLE FOR ANY CLAIM, OR ANY
SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR ANY DAMAGES WHATSOEVER
RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF
CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN
CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.

Except as contained in this notice, the name of a copyright holder
shall not be used in advertising or otherwise to promote the sale, use
or other dealings in this Software without prior written authorization
of the copyright holder.


All trademarks and registered trademarks mentioned herein are the
property of their respective owners.
```
