# KanjiVariants

MJ縮退マップに基づく漢字代替候補の検索・置換、常用漢字表の音訓索引、学年別漢字配当表を参照できる .NET 6 ライブラリです。JIS X 0208 / JIS X 0213 の文字集合を対象にできます。

異体字機能は、MJ縮退マップに基づく漢字代替候補の検索と置換を目的としています。Shift-JISのバイト列を読み書きするエンコーダー／デコーダーではなく、JIS X 0201の半角カタカナなどを含むShift-JIS全体の文字検査も行いません。また、一般的な旧字体・異体字を網羅して新字体へ変換する辞書ではありません。通常の置換は、使用するMJデータに一意な変換先が定義されている場合に行います。明示的に指定した場合だけ、登録済みIVS/SVSから基底文字へのフォールバックも行えます。常用漢字表の参照機能は、この異体字機能とは独立しています。

## 対応する CharacterSet

### `CharacterSet.JisX0208`

JIS X 0208 全体を対象とします。

- 第1水準漢字
- 第2水準漢字
- JIS X 0208 に含まれるその他の文字

第1水準漢字・第2水準漢字は別々の文字集合ではなく、JIS X 0208 内部の分類です。現時点では、第1水準のみ、または第2水準のみを個別に指定するAPIはありません。

### `CharacterSet.JisX0213Plane1`

JIS X 0213 第1面だけを対象とします。第1面にはJIS X 0208由来の文字に加えて、JIS X 0213で追加された文字もあります。そのため、JIS X 0213 第1面とJIS X 0208は同じ集合ではありません。

### `CharacterSet.JisX0213Plane2`

JIS X 0213 第2面だけを対象とします。第1面の文字は、`JisX0213Plane2` では使用可能（supported）になりません。

### `CharacterSet.JisX0213`

JIS X 0213 第1面または第2面に含まれる文字を対象とします。概念的には、`JisX0213Plane1` または `JisX0213Plane2` のいずれかに含まれる文字集合です。

第1水準／第2水準はJIS X 0208内の漢字分類、第1面／第2面はJIS X 0213の面を表します。したがって、第1水準と第1面、第2水準と第2面はそれぞれ別の概念です。

## 使い方

JIS X 0208を対象にする場合は、これまでどおり `CharacterSet.JisX0208` を指定します。

```csharp
using KanjiVariants;

var candidates = Kanji.GetAlternatives("髙", CharacterSet.JisX0208);
var converted = KanjiText.Replace("髙橋𠮷野", CharacterSet.JisX0208);
// converted == "高橋吉野"
var usable = KanjiText.IsSupported(converted, CharacterSet.JisX0208);
```

JIS X 0213の面ごと、または両面を対象にできます。

```csharp
var supportedPlane1 = Kanji.IsSupported("丑", CharacterSet.JisX0213Plane1);
var supportedPlane2 = Kanji.IsSupported("丑", CharacterSet.JisX0213Plane2);
var supportedJisX0213 = Kanji.IsSupported("丑", CharacterSet.JisX0213);

// supportedPlane1 == true
// supportedPlane2 == false
// supportedJisX0213 == true
```

この例では、bare Unicodeコードポイントの `丑`（U+4E11）はJIS X 0213第1面に含まれるものとして判定されます。同じUnicodeコードポイントに異なる字形のMJ文字がある場合、その所属は対応する代表情報に基づいて判定されます。

入力文字列に異なる文字集合由来の文字が混在していても、特別なAPIは必要ありません。指定する `CharacterSet` は入力の出自ではなく、変換後に使用可能としたい文字集合です。

```csharp
var jis0208Text = KanjiText.Replace(input, CharacterSet.JisX0208);
// JIS X 0208に含まれる文字は維持します。
// 含まれない文字は、一意な代替文字がJIS X 0208にあれば置換します。
// 代替できない文字は元のまま残します。

var jis0213Text = KanjiText.Replace(input, CharacterSet.JisX0213);
// JIS X 0213の第1面または第2面で使用可能な文字は維持します。
```

`Replace` は、入力文字が指定された文字集合で使用可能ならそのまま維持します。使用できない場合はMJ縮退マップの一意な変換先を調べ、変換先が指定された `CharacterSet` に含まれる場合に置換します。置換先が使えない、または一意な変換先がない場合は、元の文字を残します。

### IVS/SVS

IVS/SVSは、漢字の字形を区別するためにVariation Selectorを後ろに付けた表記です。登録済みのIVS/SVSは一つの漢字表現として解析し、代替候補を調べられます。Variation Sequence自体は、`JisX0208`、`JisX0213Plane1`、`JisX0213Plane2`、`JisX0213` のいずれでも supported とは判定されません。

一点しんにょうの字形を指定する「辻󠄀」は、`辻`（U+8FBB）とVariation Selector（U+E0100）の登録済みシーケンスです。基底文字へのフォールバックは、`KanjiFallbackOptions.AllowVariationSelectorFallback` を指定した場合だけ行います。

```csharp
var options = KanjiFallbackOptions.AllowVariationSelectorFallback;
var result = KanjiText.Replace("辻\U000E0100", CharacterSet.JisX0208, options);
// result == "辻"
```

MJ縮退マップの一意な変換先が指定された文字集合で使用可能なら、Variation Selectorを除くフォールバックより優先します。一意な変換先が使えない場合でも、基底文字が指定された `CharacterSet` に含まれなければフォールバックしません。未登録のVariation Sequenceもフォールバック対象外です。`KanjiFallbackOptions.None` ではVariation Selectorを削除しません。フォールバックではVariation Selectorが指定していた字形情報が失われます。また、フォールバックした基底文字を、同じ呼び出しの中でさらにMJ縮退マップにより置換することはありません。

### CP932（Windows-31J）などの対象外文字

`CharacterSet.JisX0208` が対象とするのはJIS X 0208に定義された文字です。CP932（Windows-31J）はJIS X 0208を基礎にした符号化方式ですが、独自の拡張文字を含みます。また、JIS X 0213ではJIS X 0208にない文字も追加されています。これらをJIS X 0208の文字と同一視することはありません。

文字が指定された `CharacterSet` に含まれない場合、`Kanji.IsSupported` はその漢字表現を、`KanjiText.IsSupported` は文字列中の該当文字を supported ではないと判定します。`KanjiText.Replace` は対象外文字を自動的に別の文字へ変換するものではなく、MJ縮退マップに対応があり、一意な変換先が指定された `CharacterSet` に含まれる場合に限り置換します。

## 常用漢字表の音訓索引

`JoyoKanji` では、文化庁の本表字2136字について、旧字体等、音訓、語例、備考を参照できます。検索時だけひらがな・カタカナの差を吸収し、返す読みは原典表記のままです。読みの検索は完全一致です。

```csharp
var entry = JoyoKanji.Get("亜");
Console.WriteLine(entry?.OldForm); // 亞

foreach (var reading in JoyoKanji.GetReadings("高"))
    Console.WriteLine($"{reading.Type}: {reading.Reading}");

var matches = JoyoKanji.FindByReading("こう"); // 「コウ」も検索可能
var onOnly = JoyoKanji.FindByReading("こう", KanjiReadingType.On);
```

本表に載る字体だけを常用漢字として扱います。`JoyoKanji.IsJoyo("亞")` と `JoyoKanji.IsJoyo("髙")` は `false` です。登録済みIVS/SVSも対象外です。`JoyoKanji` は異体字を自動縮退しません。必要に応じ、`Kanji.TryGetAlternative(...)` の結果を明示的に渡してください。

`OldForm` は併記が1字のときだけ設定されます。「弁」のように複数ある場合は `OldForm == null` で、すべての候補を `OldForms` から参照できます。角括弧による字形注記を含む原典の文字欄は `SourceLabel` に保持します。備考欄は字単位の情報なので、各 `KanjiReading.Note` に同じ原文を入れています。どの音訓だけに適用されるかは推定していません。

## 学年別漢字配当表（教育漢字）

`EducationKanji` は、平成29年告示の小学校学習指導要領に掲載された配当漢字1,026字を、学年別に参照します。`GetByGrade()` は転記データの順で、共有の読み取り専用一覧を返します。

```csharp
bool isEducationKanji = EducationKanji.IsEducationKanji("学"); // true
KanjiGrade? grade = EducationKanji.GetGrade("学");          // Grade1
var firstGrade = EducationKanji.GetByGrade(KanjiGrade.Grade1); // 80字
```

配当表の文字そのものだけを判定します。旧字体、異体字、IVS/SVS付きの表現は対象外です。`EducationKanji` は `JoyoKanji` や異体字変換と自動的に結合しません。変換が必要な場合は、利用者側で `Kanji.TryGetAlternative(...)` などと明示的に組み合わせてください。

## API の使い分け

| API | 用途 | 文字列入力で許可する内容 |
|---|---|---|
| `Kanji.GetAlternatives` | MJ縮退マップにある候補を一覧取得。オプション指定時は登録済みIVS/SVSの基底文字も候補に追加 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.TryGetAlternative` | MJ縮退マップの一意な変換先を優先。使用できない場合、オプション指定時は登録済みIVS/SVSの基底文字を取得 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.IsSupported` | 漢字表現そのものが指定された `CharacterSet` に含まれるか確認 | 漢字一文字、または登録済み IVS/SVS |
| `KanjiText.Replace` | 変換可能な漢字を文字列内で置換。オプション指定時は登録済みIVS/SVSの基底文字へフォールバック | 任意の文字列 |
| `KanjiText.IsSupported` | 文字列全体を指定された `CharacterSet` で検証 | 任意の文字列 |

`GetAlternatives` は候補一覧、`TryGetAlternative` は一意変換表を優先した結果です。候補一覧が1件でも、それだけで一意な変換先とは判断しません。`KanjiCharacter.Parse` は形式が不正なら `FormatException`、`TryParse` は `false` を返します。

## 一意な変換先の一覧

MJ縮退マップの一意な変換表から作成した一覧を [`data/MJUniqueAlternatives.1.2.0.csv`](data/MJUniqueAlternatives.1.2.0.csv) として同梱しています。各行はMJ文字図形名ごとの変換元表現と一意な変換先を示し、登録済みIVS/SVSも別行で掲載します。同じUnicode表現が複数行に現れる場合があります。MJ文字図形名で区別される字形ごとに変換先が異なることがあるためです。この一覧は特定の文字集合への収録可否で絞り込んでいません。変換先のUCSと、データに記録がある場合はJIS X 0213の面区点位置を掲載しています。

## 文字列処理のルール

- ASCIIは使用可能として通過します。ASCII以外の文字は、指定された `CharacterSet` への収録を確認します。
- 通常のVariation Selectorを伴わないUnicodeコードポイントは、そのコードポイントの文字集合所属を確認します。
- 登録済みIVS/SVSは一つの単位として扱いますが、シーケンス自体はsupportedではありません。未登録Variation Sequenceもsupportedではなく、基底文字だけを切り離して判定しません。
- `Replace` は指定された文字集合で使用可能な文字を維持します。使用できない文字はMJ縮退マップの一意な変換先を確認し、変換先が指定集合に含まれる場合に置換します。
- 一意な変換先が使えず、登録済みIVS/SVSに `AllowVariationSelectorFallback` が指定されている場合は、基底文字が指定集合に含まれるときに基底文字へフォールバックします。未登録シーケンスや、指定集合に含まれない基底文字はフォールバックしません。
- 一意な変換先と基底文字へのフォールバックのどちらも使えない場合は、元の文字を残します。フォールバック後の基底文字を同じ呼び出しの中でさらにMJ縮退しません。
- 置換後の文字列全体を検証するには `KanjiText.IsSupported` を使います。置換できない文字が残る場合、結果全体はsupportedにならないことがあります。
- `Replace` は不正なUTF-16を例外にせず保持して走査を続けます。`IsSupported` は不正なUTF-16があれば `false` を返します。
- `Replace` と `IsSupported` にnullを渡すと `ArgumentNullException` が発生します。

```csharp
if (Kanji.TryGetAlternative("髙", CharacterSet.JisX0208, out var replacement))
    Console.WriteLine(replacement); // 高

var result = KanjiText.Replace(input, CharacterSet.JisX0213);
if (!KanjiText.IsSupported(result, CharacterSet.JisX0213))
    Console.WriteLine("置換できない文字が残っています。");
```

公開メソッドの引数、戻り値、例外の説明はXMLドキュメントコメントにも記載しています。`dotnet build` で `KanjiVariants.xml` が生成されます。

## ビルドと検証

```powershell
dotnet build KanjiVariants.slnx -c Release
dotnet test KanjiVariants.Tests/KanjiVariants.Tests.csproj -c Release
```

ユニットテストにはxUnitを使用しています（`xunit.v3.mtp-off` 4.0.0）。MJ等の生成データを更新するには、Python 3 で `python tools/generate.py` を実行します。常用漢字データは固定した `data/JoyoKanjiOnkunIndex.html` から `python tools/generate_joyo.py` で再生成できます。原典の取得を更新する場合だけ `python tools/fetch_joyo.py` を明示的に実行します。実行時のネットワーク接続は不要です。

教育漢字データは `python tools/generate_education_kanji.py` で再生成し、`--check` で生成済みファイルとの一致を確認できます。平成29年告示版の原典PDFは画像の表なので、その転記を `data/EducationKanjiGradeTable.txt` に固定しています。Generatorは固定PDFのSHA-256と転記の件数・重複を検証します。原典の更新取得は `python tools/fetch_education_kanji.py` で明示的に行い、PDFが変わった場合は転記を再照合してください。

性能計測は別プロジェクトで実行します。`dotnet run --project KanjiVariants.Benchmarks/KanjiVariants.Benchmarks.csproj -c Release -- --filter "*"` はBenchmarkDotNetを復元し、検索・置換とメモリ割り当てを計測します。

## データと出典

生成元データを `data/` に固定し、実行時には生成済みデータを使います。JIS X 0213の所属情報は、MJ文字情報一覧表 Ver.006.02の「実装したUCS」と「X0213」に基づいています。

| データ | バージョン・出典 |
|---|---|
| MJ文字情報一覧表 | Ver.006.02、[文字情報技術促進協議会](https://moji.or.jp/mojikiban/mjlist/) |
| MJ縮退マップ、MJ縮退マップ 一意な変換表 | Ver.1.2.0、[文字情報技術促進協議会](https://moji.or.jp/mojikiban/map/) |
| Unicode IVD | 2026-08-03、[Unicode IVD](https://www.unicode.org/ivd/data/2026-08-03/) |
| Unicode Standardized Variants | Unicode 18.0.0、[Unicode Character Database](https://www.unicode.org/Public/18.0.0/ucd/StandardizedVariants.txt) |
| JIS X 0208 の Unicode 対応 | Pythonの `euc_jp` デコーダーで区点1–94を走査して生成。Windows CP932の拡張文字は含めない。 |
| 常用漢字表の音訓索引 | [文化庁「常用漢字表の音訓索引」](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/kanji/joyokanjisakuin/index.html)。公開HTMLを固定し、本表の文字・音訓・語例・備考をKanjiVariants用に抽出・再構成。 |
| 学年別漢字配当表 | [文部科学省「小学校学習指導要領（平成29年告示）」別表「学年別漢字配当表」](https://www.mext.go.jp/content/20230120-mxt_kyoiku02-100002604_01.pdf)。公式PDFを固定し、画像表を転記して検索表へ加工。各学年80・160・200・202・193・191字。 |

### 学年別漢字配当表の照合結果

2026-09-29に、`data/EducationKanjiGradeTable.txt` の**収録文字と所属学年**を、文部科学省の公開資料から独立に再構成して照合しました。照合対象の平成29年版PDFのSHA-256は `6AF90F134B243E44F9767C37EE3079FAC092883FD6359B836A5733DD25B43902` で、TXTに記録した値と一致しています。

1. [平成10年12月告示・平成15年12月一部改正の配当表](https://www.mext.go.jp/a_menu/shotou/cs/1320015.htm)から、HTMLの表を直接読み取り、旧版の1006字を学年別に取得しました。
2. [平成20年3月告示の小学校学習指導要領](https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2010/11/29/syo.pdf)と、[平成27年3月一部改正後の同指導要領](https://www.mext.go.jp/a_menu/shotou/new-cs/youryou/__icsFiles/afieldfile/2015/03/26/1356250_1.pdf)の配当表を比較しました。後者はPDFのテキストレイヤーから直接抽出でき、平成10年版HTMLと全6学年で文字集合・掲載順が一致しました。平成20年告示当初のPDFもWeb上の可読テキストでは一致しましたが、そのWeb側の抽出方式は確認できていません。
3. [平成29年告示の解説・国語編](https://www.mext.go.jp/content/20220606-mxt_kyoiku02-100002607_002.pdf)のPDFテキストレイヤーから、新規追加20字と既存字の学年移動を取得しました。追加20字の一覧は、[移行措置の概要](https://www.mext.go.jp/a_menu/shotou/new-cs/__icsFiles/afieldfile/2019/01/16/1387780_005_003_1.pdf)のテキストレイヤーとも一致しました。[国語に関するQ&A](https://www.mext.go.jp/content/1422304_001.pdf)も、20字の追加と32字の負担調整を説明しています。
4. 旧版の各学年に公式資料の変更を適用して1026字を再構成し、TXTと学年別の集合を機械比較しました。

| 対象 | 第1学年 | 第2学年 | 第3学年 | 第4学年 | 第5学年 | 第6学年 | 合計 | ユニーク | 重複 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 旧版HTML | 80 | 160 | 200 | 200 | 185 | 181 | 1006 | 1006 | 0 |
| 再構成した平成29年版 | 80 | 160 | 200 | 202 | 193 | 191 | 1026 | 1026 | 0 |
| `EducationKanjiGradeTable.txt` | 80 | 160 | 200 | 202 | 193 | 191 | 1026 | 1026 | 0 |

新旧の全体集合の差は、公式解説にある新規追加20字（茨・媛・岡・潟・岐・熊・香・佐・埼・崎・滋・鹿・縄・井・沖・栃・奈・梨・阪・阜）のみで、旧版から削除された字はありません。既存字の学年変更は、都道府県名への対応で第4学年へ移した5字と、負担調整で移行した32字の計37字です。移動の内訳は5→4が4字、6→4が1字、4→5が21字、4→6が2字、5→6が9字で、公式解説と一致しました。再構成版に対するTXTの不足・余分・学年違い・重複はいずれも**0字**でした。

この照合では1026字全体をOCRして作り直していません。今回、旧版HTMLとPDFテキストレイヤーから取得できる情報の検証にOCRは使用していません。平成29年版PDFに対するTXTの**掲載順の一字単位の完全一致は未検証**です。`GetByGrade()` の列挙順はTXTの転記順であり、利用時に原典の掲載順との厳密な一致を前提にしないでください。

### 日本語IVSの閲覧用一覧

[`data/Japanese_IVS_2026-08-03.xlsx`](data/Japanese_IVS_2026-08-03.xlsx) は、[Unicode IVD 2026-08-03](https://www.unicode.org/ivd/data/2026-08-03/IVD_Sequences.txt) からMoji_Joho、Adobe-Japan1、Hanyo-DenshiのCollectionを抽出した閲覧用Excelファイルです。`BaseCharacter` 列と `IVSCharacter` 列には、[IPAmj明朝](https://moji.or.jp/mojikiban/font/) を指定しています。この一覧は上記のランタイム用生成データとは別の参考資料であり、一覧への掲載だけでライブラリの対応範囲が広がるわけではありません。

IPAmj明朝の配布元は、同フォントのIVS実装が2017-12-12版のMoji_Johoコレクションに準拠すると説明しています。そのため、Adobe-Japan1やHanyo-Denshiのシーケンス、または後のIVDで追加されたシーケンスでは、Excel上で指定された字形が表示されるとは限りません。見た目が同じでもVariation Selectorがないとは判断せず、符号位置を確認してください。

MJデータの著作権者は独立行政法人情報処理推進機構（IPA）です。MJ文字情報一覧表とMJ縮退マップは [CC BY-SA 2.1 JP](https://creativecommons.org/licenses/by-sa/2.1/jp/) により提供されています。Unicodeのデータは [Unicode Terms of Use](https://www.unicode.org/terms_of_use.html) に従います。文化庁の公開情報は[文部科学省ウェブサイト利用規約](https://www.mext.go.jp/b_menu/1351168.htm)を参照してください。出典と加工の詳細は `LICENSE-NOTICES.md` に記載しています。

## 免責事項

本ライブラリおよび同梱データは現状のまま提供されます。本ライブラリまたは同梱データの利用により発生したいかなる損害についても、作者は一切責任を負いません。ライセンス条件の異なる同梱データについては、`LICENSE-NOTICES.md` もご確認ください。
