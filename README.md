# KanjiVariants

MJ縮退マップに基づく異体字・代替文字の検索・置換と、JIS X 0208 / JIS X 0212 / JIS X 0213 の文字集合判定を行える .NET 6 ライブラリです。常用漢字・教育漢字・人名用漢字、常用漢字の音訓と小学校・中学校・高等学校ごとの配当、表外読み、送り仮名、MJ文字情報も参照できます。

## 主な機能

- **異体字・代替文字**：MJ縮退マップに基づく候補検索と文字列の置換、登録済みIVS/SVSの処理
- **文字集合の判定**：JIS X 0208 / 0212 / 0213への収録状況の確認
- **漢字と読みの参照**：常用漢字・教育漢字・人名用漢字、音訓・学校段階・表外読み・送り仮名
- **文字・文章の診断**：Unicode・MJ文字情報の参照、常用漢字・学年別漢字の文章診断とCLI
- **MJ縮退関係の逆引き**：縮退先から元表現と根拠を取得。公開済みNuGet 1.2.1には未収録

## ブラウザで試す

**[KanjiVariants Playground を開く](https://koichi-kobayashi.github.io/KanjiVariants.Playground/)**

KanjiVariantsの文字列変換・診断機能を、インストール不要でブラウザから試せます。

- **文章一括変換**：JIS X 0208 / 0212 / 0213を対象に、異体字を含む文章をまとめて変換
- **文字列診断**：文字集合への収録状況と代替候補を確認
- **IVS/SVS対応**：字形指定を含む文字の診断や、基底文字へのフォールバックを体験
- **変換結果の確認**：変換前後の比較、変更箇所の表示、全文コピー

変換できない文字は元のまま残ります。入力された文章はブラウザ内で処理され、外部サーバーには送信されません。

ソースコード：[KanjiVariants.Playground](https://github.com/Koichi-Kobayashi/KanjiVariants.Playground)

## インストール

NuGetパッケージは `net6.0` 向けです。.NET 6以降のアプリケーションで利用できます。

NuGet.orgで公開済みの [KanjiVariants 1.2.1](https://www.nuget.org/packages/KanjiVariants/1.2.1) をインストールします。

> `MjFallback.GetSources` はmainブランチで利用できますが、公開済みNuGet 1.2.1には含まれていません。利用する場合はmainブランチのソースを[ビルド](#ビルドと検証)してください。

```bash
dotnet add package KanjiVariants --version 1.2.1
```

プロジェクトファイルに直接指定する場合は、`ItemGroup` 内へ追加してください。

```xml
<PackageReference Include="KanjiVariants" Version="1.2.1" />
```

## 基本的な使い方

JIS X 0208を対象にする場合は、`CharacterSet.JisX0208` を指定します。

```csharp
using KanjiVariants;

var candidates = Kanji.GetAlternatives("髙", CharacterSet.JisX0208);
var converted = KanjiText.Replace("髙橋𠮷野", CharacterSet.JisX0208);
// converted == "高橋吉野"
var usable = KanjiText.IsSupported(converted, CharacterSet.JisX0208);
```

異体字機能は、MJ縮退マップに基づく漢字代替候補の検索と置換を目的としています。Shift-JISのバイト列を読み書きするエンコーダー／デコーダーではなく、JIS X 0201の半角カタカナなどを含むShift-JIS全体の文字検査も行いません。また、一般的な旧字体・異体字を網羅して新字体へ変換する辞書ではありません。通常の置換は、使用するMJデータに一意な変換先が定義されている場合に行います。明示的に指定した場合だけ、登録済みIVS/SVSから基底文字へのフォールバックも行えます。常用漢字表の参照機能は、この異体字機能とは独立しています。

## MJ縮退関係の逆引き

> `MjFallback.GetSources` はmainブランチで利用できますが、公開済みNuGet 1.2.1には含まれていません。

`MjFallback.GetSources` は、指定文字を縮退先とするMJ縮退元表現と、その関係の根拠を取得します。縮退先から元の表現をたどり、どのMJ文字・分類に基づく候補かを確認できます。

例えば、`高`から`髙`、`斎`から`齋`、`崎`から`﨑`、`辻`から`辻＋登録済みIVS`を取得できます。ここでの「高 → 髙」は検索する方向であり、原典の縮退方向は「髙 → 高」です。

```csharp
using KanjiVariants;

var sources = MjFallback.GetSources("高");
foreach (var source in sources)
{
    Console.WriteLine(source.Character); // 髙などの縮退元表現
    foreach (var evidence in source.Evidence)
        Console.WriteLine($"{evidence.Source} → {evidence.Target}: {evidence.MjGlyphName}, {evidence.RelationKind}");
}
```

### 取得結果と根拠情報

各候補の `Character` に元表現、`Evidence` に元表現から検索文字への直接関係の根拠を保持します。同じ元表現は一つの候補に統合し、異なるMJ文字・分類による根拠は残します。

根拠には、原典上の縮退方向（`Source` → `Target`）、MJ文字図形名（`MjGlyphName`）、原典の五分類（`RelationKind`）が含まれます。五分類は、JIS包摂規準・UCS統合規則、法務省戸籍法関連通達・通知、法務省告示582号別表第四、辞書類等による関連字、読み・字形による類推です。元表現の列由来や原典の付記なども[詳細仕様](#逆引きの詳細仕様とデータ生成)で確認できます。

### 異体字検索との違い・制限事項

- 一般的な旧字体・異体字を網羅的に検索するAPIではありません。関連する文字が、必ず相互に置き換え可能とは限りません。
- 候補は元の縮退関係と根拠情報を保持します。候補を自動置換先として採用するAPIではありません。
- `Kanji.GetAlternatives` は指定文字からMJ縮退マップの代替候補を取得します。`MjFallback.GetSources` は逆に、指定文字へ縮退する元表現を取得します。既存の `Kanji.GetAlternatives` の動作は変更していません。
- 原典に記録された直接関係だけを逆引きし、正規化・推移展開・JIS文字集合での絞り込みは行いません。

### 逆引きの詳細仕様とデータ生成

**入力・戻り値・例外**

- `GetSources(string target)` は `KanjiCharacter.Parse` が認識する一文字、または登録済みIVS/SVSを受け付けます。漢字に加え、MJデータに登録された `〆` なども対象です。`GetSources(KanjiCharacter target)` オーバーロードもあります。
- 戻り値は `IReadOnlyList<MjFallbackSource>` です。有効な表現で該当なしの場合は空の一覧です。原典の縮退先はVSなしのため、登録済みVS付き入力も空です。既定値の `KanjiCharacter` も空です。
- 文字列入力がnullの場合は `ArgumentNullException`、`KanjiCharacter.Parse` が受理しない入力（空文字・複数文字・不正UTF-16・未登録VSなど）の場合は `FormatException` です。
- 自己候補は除外し、基底コードポイント、VSの順に並べます。根拠もMJ番号・分類等で固定順です。
- 結果と根拠は変更不能です。検索済みの結果を遅延キャッシュして共有し、連続検索で候補を再構築しません。初回は生成済み配列の初期化と該当結果の構築だけで、MJ全文の解析は行いません。

**元表現と原典の列情報**

元表現には実装UCS・互換漢字・登録済みIVS/SVSを使います。実装UCS・互換漢字のない行は対応UCSを補助表現に使い、`SourceRepresentationKind`（`MjCharacterMatchKind`）で区別します。対応UCSと実装UCSを同一視しません。

| 根拠のプロパティ | 内容 |
|---|---|
| `SourceRepresentationKind` | MJ列から元表現を採用した理由 |
| `JisX0213` | 原典の縮退先の面区点 |
| `NotificationType` | 原典の通知種別 |
| `HopCount` | 原典のホップ数。このAPIが推移検索した回数ではない |
| `Note` / `Table` / `Rank` | 原典の付記・表・順位 |

原典の面区点・通知種別・ホップ数・付記・表・順位に記録がない場合、対応するプロパティは `null` です。

**データ生成**

既存の [MJ文字情報一覧表 Ver.006.02](data/MJ/mji.00602.xlsx) と [MJ縮退マップ Ver.1.2.0](data/MJ/MJShrinkMap.1.2.0.json) から、[生成器](tools/generate_mj_fallback.py)で直接関係の逆引き索引を作成します。登録済みシーケンスは既存の固定Unicodeデータと照合し、新しい外部データは追加していません。

```bash
python tools/generate_mj_fallback.py
python tools/generate_mj_fallback.py --check
python tools/check_generated.py
```

順に、再生成、生成済み逆引き索引の非更新検証、全生成物の検証です。逆引き索引は既存MJ原典由来の加工データとしてCC BY-SA 2.1 JPの条件を維持します。独自C#コードのMITライセンスとは区別します。出典・加工・適用範囲は[データと出典](#データと出典)と [LICENSE-NOTICES.md](LICENSE-NOTICES.md#mj加工データのライセンス範囲) を参照してください。

## 詳細機能とAPIリファレンス

- [対応する文字集合](#対応する-characterset)・[文字集合を指定した変換](#文字集合を指定した変換)・[IVS/SVS](#ivssvs)
- [常用漢字と音訓](#常用漢字表の音訓索引)・[表外読み](#表外読み)・[人名用漢字](#人名用漢字)・[教育漢字](#学年別漢字配当表教育漢字)
- [文章診断CLI](#文章診断cliサンプルツール)・[音訓と学校段階](#常用漢字の音訓と学校段階)・[送り仮名](#送り仮名)
- [MJ文字情報](#mj文字情報)・[文字診断](#文字診断)・[APIの使い分け](#api-の使い分け)・[文字列処理のルール](#文字列処理のルール)
- [APIドキュメント](#apiドキュメント)

### 対応する CharacterSet

#### `CharacterSet.JisX0208`

JIS X 0208 全体を対象とします。

- 第1水準漢字
- 第2水準漢字
- JIS X 0208 に含まれるその他の文字

第1水準漢字・第2水準漢字は別々の文字集合ではなく、JIS X 0208 内部の分類です。現時点では、第1水準のみ、または第2水準のみを個別に指定するAPIはありません。

#### `CharacterSet.JisX0212`

JIS X 0212-1990はJIS X 0208を補う独立した補助文字集合です。補助漢字5,801字と非漢字266字、計6,067文字を対象とし、JIS X 0208は自動的に含めません。JIS X 0213とも別の集合として、実データから独立して判定します。

`Kanji.IsSupported` は既存の `KanjiCharacter` が解析できる漢字表現を判定します。非漢字を含む文字列は `KanjiText.IsSupported` で検査できます。同APIは既存仕様としてASCIIも使用可能と扱うため、厳密なJIS X 0212の符号化可否検査ではありません。例えば「Ä」は文字列APIでは判定できますが、漢字APIでは `FormatException` です。登録済みIVS/SVSそのものは、この集合でもsupportedになりません。

```csharp
bool jis0212 = Kanji.IsSupported("丂", CharacterSet.JisX0212); // true
bool jis0208 = Kanji.IsSupported("丂", CharacterSet.JisX0208); // false
bool textSupported = KanjiText.IsSupported("丂丄Ä", CharacterSet.JisX0212); // true
```

固定したUnicode ICUのJIS X 0212対応表（6,067文字）を使用し、漢字5,801字のコードポイントと区点をUnihan 18.0.0の `kJis1` と全件照合しています。全6,067文字がBMP内です。現行のJIS X 0208所属表との重複は0文字、MJ由来のJIS X 0213所属表との重複は2,751文字です。

#### `CharacterSet.JisX0213Plane1`

JIS X 0213 第1面だけを対象とします。第1面にはJIS X 0208由来の文字に加えて、JIS X 0213で追加された文字もあります。そのため、JIS X 0213 第1面とJIS X 0208は同じ集合ではありません。

#### `CharacterSet.JisX0213Plane2`

JIS X 0213 第2面だけを対象とします。第1面の文字は、`JisX0213Plane2` では使用可能（supported）になりません。

#### `CharacterSet.JisX0213`

JIS X 0213 第1面または第2面に含まれる文字を対象とします。概念的には、`JisX0213Plane1` または `JisX0213Plane2` のいずれかに含まれる文字集合です。

第1水準／第2水準はJIS X 0208内の漢字分類、第1面／第2面はJIS X 0213の面を表します。したがって、第1水準と第1面、第2水準と第2面はそれぞれ別の概念です。

### 文字集合を指定した変換

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

#### IVS/SVS

IVS/SVSは、漢字の字形を区別するためにVariation Selectorを後ろに付けた表記です。登録済みのIVS/SVSは一つの漢字表現として解析し、代替候補を調べられます。Variation Sequence自体は、`JisX0208`、`JisX0212`、`JisX0213Plane1`、`JisX0213Plane2`、`JisX0213` のいずれでも supported とは判定されません。

一点しんにょうの字形を指定する「辻󠄀」は、`辻`（U+8FBB）とVariation Selector（U+E0100）の登録済みシーケンスです。基底文字へのフォールバックは、`KanjiFallbackOptions.AllowVariationSelectorFallback` を指定した場合だけ行います。

```csharp
var options = KanjiFallbackOptions.AllowVariationSelectorFallback;
var result = KanjiText.Replace("辻\U000E0100", CharacterSet.JisX0208, options);
// result == "辻"
```

MJ縮退マップの一意な変換先が指定された文字集合で使用可能なら、Variation Selectorを除くフォールバックより優先します。一意な変換先が使えない場合でも、基底文字が指定された `CharacterSet` に含まれなければフォールバックしません。未登録のVariation Sequenceもフォールバック対象外です。`KanjiFallbackOptions.None` ではVariation Selectorを削除しません。フォールバックではVariation Selectorが指定していた字形情報が失われます。また、フォールバックした基底文字を、同じ呼び出しの中でさらにMJ縮退マップにより置換することはありません。

#### CP932（Windows-31J）などの対象外文字

`CharacterSet.JisX0208` が対象とするのはJIS X 0208に定義された文字です。CP932（Windows-31J）はJIS X 0208を基礎にした符号化方式ですが、独自の拡張文字を含みます。また、JIS X 0213ではJIS X 0208にない文字も追加されています。これらをJIS X 0208の文字と同一視することはありません。

文字が指定された `CharacterSet` に含まれない場合、`Kanji.IsSupported` はその漢字表現を、`KanjiText.IsSupported` は文字列中の該当文字を supported ではないと判定します。`KanjiText.Replace` は対象外文字を自動的に別の文字へ変換するものではなく、MJ縮退マップに対応があり、一意な変換先が指定された `CharacterSet` に含まれる場合に限り置換します。

### 常用漢字表の音訓索引

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

#### 常用漢字の文章診断

`JoyoKanjiDiagnostics.AnalyzeText` は、文章中の漢字が常用漢字表の本表字そのものかを診断します。

```csharp
var result = JoyoKanjiDiagnostics.AnalyzeText(
    "明日は髙橋さんと會議です。");
Console.WriteLine(result.ContainsOnlyJoyoKanji); // false
foreach (var item in result.NonJoyoItems)
{
    Console.WriteLine($"{item.Utf16Index}: {item.Original} Reason={item.Reason}");
}
// 3: 髙 Reason=NotJoyoKanji
// 8: 會 Reason=NotJoyoKanji
```

`Items` は入力全体を論理単位順に保持し、`NonJoyoItems` は `NotJoyoKanji` の項目だけを入力順に返します。両方とも読み取り専用で、同じ項目を共有します。`Text` は入力stringそのものです。項目の `IsJoyoKanji` は本表字だけtrueで、かな・英数字・記号・絵文字・`々`・`〆`・`〻` などは `NotKanji` として保持し、文章全体の違反扱いにはしません。

旧字体・異体字・互換漢字は自動変換・正規化せず、MJ縮退マップも自動利用しません。漢字と直後のVSは登録有無によらず一項目で、IVS/SVSを基底文字へ自動フォールバックせず `NotJoyoKanji` とします。必要な異体字変換は利用者側で明示的に行ってから診断してください。

論理単位はUnicodeの書記素クラスタではなく、補助文字を含む1スカラーと直後のVSをまとめた単位です。`Utf16Index` は0始まりのUTF-16コード単位位置、`Utf16Length` はその長さです。CRとLFは別項目で、不正UTF-16は1コード単位ずつ `InvalidUtf16` として元の内容と位置を保持し、`NonJoyoItems` には含めません。

`HasNonJoyoKanji` は `NotJoyoKanji`、`HasInvalidUtf16` は `InvalidUtf16` を一つでも含む場合にtrueです。`ContainsOnlyJoyoKanji` は全項目が `JoyoKanji` または `NotKanji` の場合だけtrueで、空文字もtrueです。不正UTF-16だけでもfalseとなります。null入力は `ArgumentNullException` です。

### 表外読み

`JoyoReading` は、既存の常用漢字表の音訓データを使って、漢字と読みの組み合わせの掲載有無を判定します。`IsListedReading()` は本表字と掲載音訓の組み合わせなら `true`、`IsHyogaiReading()` は漢字が本表字であり、その読みが掲載音訓に含まれなければ `true` です。漢字自体が非常用漢字の場合は、どちらも `false` になります。

```csharp
bool listed = JoyoReading.IsListedReading("高", "こう");       // true（掲載音訓は「コウ」）
bool hyogai = JoyoReading.IsHyogaiReading("愛", "いとしい"); // true（掲載音訓は「アイ」）
```

表外読みは「常用漢字表に掲載されていない読み」を示すだけで、その読みが誤りであることを意味しません。入力が実際に使われる読みかどうかも判定しません。比較には `JoyoKanji.IsReadingSupported()` を再利用し、NFC正規化後にカタカナU+30A1～U+30F6をひらがなへ変換して完全一致で調べます。長音記号の展開、活用形の展開、送り仮名補正、部分一致、読みの類推は行いません。

旧字体・異体字・IVS/SVS付き表現は対象外で、自動変換や基底文字へのフォールバックは行いません。characterがnull・空・複数文字・非漢字の場合は `false`、readingがnullなら文字が対象外でも `ArgumentNullException` です。空のreadingは未掲載として扱うため、本表字では `IsListedReading()` が `false`、`IsHyogaiReading()` が `true` になります。必要な異体字変換や読みの入力検証は、利用者側で明示的に組み合わせてください。

### 人名用漢字

`JinmeiyoKanji` は、MJ文字情報一覧表で「漢字施策 = 人名用漢字」とされた863字の「実装したUCS」を参照します。`IsJinmeiyoKanji` はその863字だけを判定します。`IsNameUsableKanji` は、常用漢字の本表字または人名用漢字に含まれる漢字を判定します。ひらがな・カタカナなど、名前に使える漢字以外の文字を判定するAPIではありません。

```csharp
bool jinmeiyo = JinmeiyoKanji.IsJinmeiyoKanji("丑"); // true
bool usable = JinmeiyoKanji.IsNameUsableKanji("高"); // true（常用漢字）
var all = JinmeiyoKanji.GetAll(); // 人名用漢字863字の共有一覧
```

旧字体・異体字を自動変換せず、IVS/SVS付き表現も基底文字へ自動フォールバックしません。判定対象は入力された文字そのものです。必要なら利用者が `Kanji.TryGetAlternative(...)` などで明示的に変換してから判定してください。

### 学年別漢字配当表（教育漢字）

`EducationKanji` は、平成29年告示の小学校学習指導要領に掲載された配当漢字1,026字を、学年別に参照します。`GetByGrade()` は転記データの順で、共有の読み取り専用一覧を返します。

```csharp
bool isEducationKanji = EducationKanji.IsEducationKanji("学"); // true
KanjiGrade? grade = EducationKanji.GetGrade("学");          // Grade1
var firstGrade = EducationKanji.GetByGrade(KanjiGrade.Grade1); // 80字
```

配当表の文字そのものだけを判定します。旧字体、異体字、IVS/SVS付きの表現は対象外です。`EducationKanji` は `JoyoKanji` や異体字変換と自動的に結合しません。変換が必要な場合は、利用者側で `Kanji.TryGetAlternative(...)` などと明示的に組み合わせてください。

#### 教育漢字の文章診断

`EducationKanjiDiagnostics.AnalyzeText` は、指定した小学学年までに習う漢字で文章が構成されているかを診断します。字幕、ゲーム内テキスト、教材、ローカライズチェックなどから利用できます。

```csharp
var result = EducationKanjiDiagnostics.AnalyzeText(
    "明日は会議を開催します。", KanjiGrade.Grade3);
Console.WriteLine(result.ContainsOnlyLearnedKanji); // false
Console.WriteLine(result.HighestEducationKanjiGrade?.ToString() ?? "-"); // Grade4
Console.WriteLine(result.HasNonEducationKanji); // true（催が配当表外）

foreach (var item in result.UnlearnedItems)
{
    Console.WriteLine($"{item.Utf16Index}: {item.Original} " +
        $"Grade={item.Grade?.ToString() ?? "-"}, Reason={item.Reason}");
}
// 4: 議 Grade=Grade4, Reason=LearnedInLaterGrade
// 7: 催 Grade=-, Reason=NotEducationKanji
```

`Items` は入力全体を論理単位ごとに保持します。ここでいう論理単位はUnicodeの書記素クラスタではなく、補助文字を含む1スカラーと、その直後にある場合のVariation Selectorをまとめた単位です。補助文字は一項目、基底文字と直後のVSも登録有無によらず一項目です。CRとLFは別項目です。不正UTF-16は1コード単位ごとに扱います。

`UnlearnedItems` は後の学年で習う漢字と配当表外の漢字だけを入力順に返します。`Items` と `UnlearnedItems` は両方とも読み取り専用です。`Grade` は実際の配当学年、`Utf16Index` は0始まりのUTF-16コード単位位置、`Utf16Length` はその長さです。

`HighestEducationKanjiGrade` は、文章中で教育漢字として判定できた項目の最高配当学年です。指定した対象学年とは独立しており、配当表外漢字が混ざっていても教育漢字部分の最高学年を返します。教育漢字が一文字もなければnullです。漢字以外、不正UTF-16、`NotEducationKanji` の項目は計算対象外です。この値だけでは、文章全体がその学年までの教育漢字で構成されていることを保証しません。

`HasNonEducationKanji` は、`Reason` が `NotEducationKanji` の項目を一つでも含む場合にtrueです。漢字のVS付き表現も含みますが、漢字以外や不正UTF-16だけではtrueになりません。

| 入力 | HighestEducationKanjiGrade | HasNonEducationKanji |
|---|---|---|
| `あいうえお`・空文字 | null | false |
| `学校` | Grade1 | false |
| `学校催` | Grade1 | true |
| `催` | null | true |

旧字体・異体字・互換漢字・IVS/SVSを自動変換しません。漢字のVS付き表現は基底文字が教育漢字でも `NotEducationKanji` で、`Grade` はnullです。変換したい場合は、利用側で明示的に `KanjiText.Replace` などを使ってから診断してください。

漢字以外（`々`・`〆`・`〻`、かな、英数字、記号、絵文字など）は `NotKanji` で、`IsLearned` はtrueです。不正UTF-16は `InvalidUtf16` として位置付きで残し、`IsLearned` はfalseですが、`UnlearnedItems` には含めません。`HasInvalidUtf16` は不正UTF-16が一つでもある場合にtrueとなり、`HasNonEducationKanji` とは独立した判定です。CLIのJSON項目 `hasInvalidUtf16` も同じ意味です。`ContainsOnlyLearnedKanji` は全項目が `NotKanji` または `Learned` ならtrueで、空文字もtrueです。個人の習得状況や読みの難しさを判定するものではありません。

null入力は `ArgumentNullException`、未定義の対象学年は `ArgumentOutOfRangeException` です。対象学年は第1～第6学年から指定します。

### 文章診断CLI（サンプル・ツール）

`KanjiVariants.Cli` は、既存の教育漢字・常用漢字診断APIを使って `.txt` ファイル全体、または `.srt` の字幕本文をチェックする.NET Toolです。実行には.NET 10ランタイムが必要です。NuGet.orgからインストールして利用できます。

```powershell
# インストール
dotnet tool install --global KanjiVariants.Cli

# 実行
kanjivariants check sample.txt --grade 3
kanjivariants joyo sample.txt
kanjivariants check subtitle.srt --grade 3
kanjivariants joyo subtitle.srt --json

# 更新
dotnet tool update --global KanjiVariants.Cli

# アンインストール
dotnet tool uninstall --global KanjiVariants.Cli
```

NuGet.orgを使用せず、ローカルで生成したパッケージから専用ディレクトリへインストールすることもできます。

```powershell
dotnet pack KanjiVariants.Cli/KanjiVariants.Cli.csproj -c Release -o ./artifacts/nuget
dotnet tool install KanjiVariants.Cli --tool-path ./artifacts/tool-test --add-source ./artifacts/nuget --version 1.2.0
./artifacts/tool-test/kanjivariants.exe check sample.txt --grade 3
```

パッケージ内容・ローカルソースだけでのインストール・リポジトリ外からの実行は、`pwsh -File tools/verify_cli_package.ps1` で検証できます。CLIのバージョンは `Directory.Build.props` の `KanjiVariantsCliVersion` で、本体とは独立して管理します。

リポジトリから直接実行する方法も利用できます。

```powershell
dotnet run --project KanjiVariants.Cli -c Release -- check sample.txt --grade 3
dotnet run --project KanjiVariants.Cli -c Release -- check sample.txt --grade 3 --unique
dotnet run --project KanjiVariants.Cli -c Release -- check sample.txt --grade 3 --json
dotnet run --project KanjiVariants.Cli -c Release -- check sample.txt --grade 3 --unique --json

dotnet run --project KanjiVariants.Cli -c Release -- joyo sample.txt
dotnet run --project KanjiVariants.Cli -c Release -- joyo sample.txt --unique
dotnet run --project KanjiVariants.Cli -c Release -- joyo sample.txt --json
dotnet run --project KanjiVariants.Cli -c Release -- joyo sample.txt --unique --json
dotnet run --project KanjiVariants.Cli -c Release -- check subtitle.srt --grade 3
dotnet run --project KanjiVariants.Cli -c Release -- joyo subtitle.srt --json
```

`check <file.txt> --grade <1-6> [--unique] [--json]` は指定した小学学年までに習う漢字かを診断します。通常出力は英語です。`--unique` は文字表現・配当学年・理由が同じ未習漢字を出現順にまとめ、件数を表示します。`--json` は標準出力にJSONだけを出力します。最高配当学年は教育漢字部分だけから計算し、教育漢字がなければ通常出力では `-`、JSONではnullです。

`check` のJSON項目は `targetGrade`、`highestEducationKanjiGrade`（学年は1～6またはnull）、`hasNonEducationKanji`、`containsOnlyLearnedKanji`、`hasInvalidUtf16`、`unlearned` です。未習項目には `text`、`grade`、`reason`、`utf16Index`、`utf16Length` を含みます。`--unique --json` では位置・長さの代わりに `firstUtf16Index` と `count` を返します。位置と長さは0始まりのUTF-16コード単位に基づきます。

`joyo <file.txt> [--unique] [--json]` は常用漢字表の本表字かを診断します。`--grade` は使用できません。旧字体・異体字・互換漢字を自動変換せず、IVS/SVSも基底文字へ変換しません。漢字以外は違反扱いしません。`--unique` は文字表現・理由で非常用漢字を集約し、最初の出現順を維持します。

`joyo` のJSON項目は `containsOnlyJoyoKanji`、`hasNonJoyoKanji`、`hasInvalidUtf16`、`nonJoyo` です。非常用項目には `text`、`reason`、`utf16Index`、`utf16Length` を含み、集約時は位置・長さの代わりに `firstUtf16Index` と `count` を返します。終了コードは **0：非常用漢字なし、1：非常用漢字あり、2：使用方法・読込・入力異常** で、不正UTF-16がある場合は2を優先します。

入力はBOMあり／なしのUTF-8だけに対応し、改行を保持します。正規化・文字置換・他の文字コードの自動判別は行いません。不正UTF-8は置換文字で補わず読込エラーとします。終了コードは **0：診断対象の問題なし、1：診断対象の問題あり、2：使用方法・読込・入力異常** です。コード1は、`check` では未習漢字あり、`joyo` では非常用漢字ありを表します。不正UTF-16を検出した場合は診断結果より優先して2を返し、診断JSONの `hasInvalidUtf16` がtrueになります（strict UTF-8のファイル読込では通常、この状態になる前に読込エラーになります）。使用方法や読込のエラーは標準エラー出力へ表示し、JSONを含め標準出力には何も出しません。

SRTでは字幕番号・タイムコードを診断せず、複数行の字幕本文だけを診断します。CRLF／LFを保持し、HTML風タグは解析・除去しません。問題文字には字幕番号、開始・終了時刻、本文内の1始まり行番号、行内の0始まりUTF-16位置を付けます。JSONの `utf16Index` は字幕本文全体での位置です。`.txt` の出力・JSON形式は従来どおりです。

SRTの通常JSON項目には `subtitleNumber`、`startTime`、`endTime`、`textLine`、`utf16IndexInLine` を追加します。`--unique` は字幕をまたいで集約し、JSONでは `firstUtf16Index` と `count` に加え、`firstSubtitleNumber`、`firstStartTime`、`firstEndTime`、`firstTextLine`、`firstUtf16IndexInLine` を返します。

入力拡張子は `.txt` と `.srt` のみ（大文字小文字を区別しません）で、どちらもUTF-8のみです。SRTの番号・時刻・本文の欠落や不正、重複番号、字幕間の区切り欠落は構文エラーとして終了コード2を返し、標準エラーへ元ファイルの行番号付きで表示します。非連番の番号、末尾空行の有無、空白だけの本文は受理します。空行だけの本文と空ファイルは構文エラーです。

### 常用漢字の音訓と学校段階

`JoyoKanjiEducation` は、文部科学省「音訓の小・中・高等学校段階別割り振り表（平成29年3月）」に基づき、常用漢字の音訓ごとの指導段階を `Elementary`・`JuniorHigh`・`HighSchool` で返します。各音訓の `Reading` は既存の `JoyoKanji` の `KanjiReading` と同じインスタンスです。同じ漢字でも読みごとに段階が異なり、漢字単位の小学校配当学年は `EducationKanji.GetGrade()` で別に調べます。この割り振りは指導の目安であり、学校での取扱いを制限するものではありません。

```csharp
var stage = JoyoKanjiEducation.GetReadingStage("衣", "ころも"); // JuniorHigh
var highSchool = JoyoKanjiEducation.GetReadingStage("悪", "オ"); // HighSchool
var readings = JoyoKanjiEducation.GetReadings("宮");
```

`IsSpecialOrLimited` は、原典で1字下げされ、「特別なもの、又は用法のごく狭いもの」とされる音訓を示します。本表字だけを対象とし、旧字体・異体字・IVS/SVSを自動変換しません。

付表1の語と付表2の都道府県名の読みは、`JoyoKanjiAppendix` から語単位で参照できます。異表記は一つの `Words` にまとめています。

```csharp
var ama = JoyoKanjiAppendix.FindByWord("海女")[0]; // Words: 海女、海士
var kagura = JoyoKanjiAppendix.FindByReading("かぐら");
```

### 送り仮名

`Okurigana` は、[文化庁「送り仮名の付け方」](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/okurikana/index.html)の通則1～7と付表に掲載された語を参照します。本則・例外・許容・付表を `OkuriganaRuleKind` で区別します。この機能は公式掲載語の参照に限定され、任意の日本語について送り仮名の正誤を自動判定するものではありません。未掲載語への規則適用、活用形の自動展開、形態素解析は行いません。

```csharp
var entries = Okurigana.Find("行う");
var alternative = Okurigana.Find("行なう"); // 同じ掲載項目を参照
var rule1 = Okurigana.GetByRule(1);
var permitted = Okurigana.GetByKind(OkuriganaRuleKind.Permitted);
var appendix = Okurigana.GetByKind(OkuriganaRuleKind.Appendix);
```

`Find()` は `Word` と `AlternativeForms` をNFC正規化後に完全一致で検索します。仮名の種類、前後の空白、活用形を同一視しません。該当なしは空一覧、nullは `ArgumentNullException` です。`GetByRule()` は1～7、不正な通則番号やenum値は `ArgumentOutOfRangeException` です。返される一覧と許容表記の一覧は、共有の読み取り専用一覧です。

`Word` は掲載語の本体、`AlternativeForms` は同じ項目の許容表記です。読みの括弧や語の構成関係は `Note` に保持し、代替表記として扱いません。通則2の許容欄では、原典が角括弧で示した表記を許容表記として扱います。同じ語が複数の通則や区分に掲載されている場合は、それぞれの項目を返します。「脅かす」のように同じ表記でも読み注記が異なる場合も保持します。

注意中の本則に関する語例は `Principle` として注記付きで収録します。`RuleNumber` は掲載箇所の番号であり、例えば通則2の注意に掲載された語は番号2と、その注意に記載された「通則1による」という説明を保持します。通則7は公式の[「本文」の見方及び使い方](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/okurikana/mikata.html)に従い、`Exception` として番号7を保持します。`《博多》織` などは `Word = "博多織"` とし、原典の《　》表記と説明を `Note` に残します。類推による語の追加は行いません。付表は `RuleNumber = null`、`Kind = Appendix` とし、送り仮名を付ける／付けない区分や許容条件は `Note` に保持します。

固定HTMLからの掲載項目数は483件です。同一語の別項目を含み、許容表記は同じ項目にまとめています。通則1～7は順に71・67・30・73・29・112・86件、付表は15件です。区分別では本則208・例外200・許容60・付表15件で、63項目に計72件の `AlternativeForms` があります。常用漢字・人名用漢字の判定や異体字変換とは独立した機能です。

### MJ文字情報

`MjCharacter` は、固定した `data/MJ/mji.00602.xlsx`（MJ文字情報一覧表 Ver.006.02）のMJ文字図形名・Unicode表現・漢字施策・X0213情報を参照します。文字図形名から一意に取得でき、Unicode表現からの検索では複数の関連MJ文字が返る場合があります。

```csharp
var entry = MjCharacter.GetByMjGlyphName("MJ028902"); // 実装したUCS: U+9AD9（髙）

foreach (var match in MjCharacter.Find("髙"))
{
    Console.WriteLine(match.Entry.MjGlyphName);
    Console.WriteLine(match.MatchKind);
}
```

「対応するUCS」と「実装したUCS」、IVS、SVS、「対応する互換漢字」はそれぞれ別の情報として保持します。`Ivs` と `Svs` は `IReadOnlyList<KanjiCharacter>` で、複数のシーケンスを原典順にすべて保持し、空欄は共有の空一覧を返します。検索インデックスには一覧内の全シーケンスを登録します。`MatchKind` は検索の一致理由を示し、同じMJ文字が複数の理由で一致した場合はビットORでまとめた1件を返します。IVS/SVSを検索しても基底文字へ自動縮退しません。結果一覧は共有の読み取り専用です。

`KanjiPolicy` は原典の「常用漢字」「人名用漢字」を表し、空欄は `null` です。既存の `JoyoKanji` / `JinmeiyoKanji` の判定とは独立しています。`JisX0213` はX0213列の原典値（例: `1-25-66`）をそのまま返し、`CharacterSet` の判定とは自動結合しません。`Find(string)` は々・〆・〻も含むUnicodeスカラー一文字を受け付け、VS付き表現には既存の登録済みIVS/SVS解析を使用します。無効な入力や該当なしは空の一覧、null入力は `ArgumentNullException` です。文字図形名の検索は大文字小文字を区別する完全一致で、該当なしは `null` です。

収録件数はMJ文字58,862件（文字図形名も58,862件）、対応UCS58,859件、実装UCS52,607件、IVS欄11,382件（シーケンス11,384件）、SVS欄89件（シーケンス89件）、漢字施策2,999件（常用2,136・人名用863）、互換漢字101件、X0213欄13,707件です。`MJ059399` と `MJ059400` はIVSを2件ずつ保持します。現行データに複数SVSの行はありませんが、SVSも複数値を扱う同じ処理を使用します。漢字施策を持つ全行の実装UCSは、既存の常用漢字・人名用漢字集合と一致することを検証しています。

### 文字診断

`KanjiDiagnostics.Analyze` で、一つの漢字表現のUnicode情報、JIS文字集合への所属、常用・教育・人名用漢字の情報、小学校・中学校・高等学校ごとの音訓配当、MJ文字情報をまとめて取得できます。既存APIの結果を集約し、旧字体やIVS/SVSを基底文字へ自動変換しません。

```csharp
var result = KanjiDiagnostics.Analyze("髙");
Console.WriteLine($"U+{result.BaseCodePoint:X}");
Console.WriteLine(result.IsJisX0208);
Console.WriteLine(result.IsJinmeiyoKanji);
```

`UnicodeScalarCount` はスカラー数、`Utf16CodeUnitCount` はUTF-16コード単位数です。音訓配当とMJ一致情報は既存APIの読み取り専用一覧を共有します。文字列入力は `KanjiCharacter.Parse` と同じく、漢字一文字または登録済みIVS/SVSだけを受理します。nullは `ArgumentNullException`、空文字・複数文字・未登録VSなどは `FormatException` です。代替候補は含まないため、必要な場合は `Kanji.GetAlternatives` を別途使用してください。

#### 文字列全体の診断

`KanjiDiagnostics.AnalyzeText` は、文字列全体について指定した `CharacterSet` への適合性と、`KanjiText.Replace` が実際に採用する置換先を文字単位で確認できます。

```csharp
var result = KanjiDiagnostics.AnalyzeText(
    "A髙橋①",
    CharacterSet.JisX0208);

Console.WriteLine(result.IsSupported); // false

foreach (var item in result.Items)
{
    Console.WriteLine(
        $"{item.Utf16Index}: {item.Original} " +
        $"Supported={item.IsSupported}, " +
        $"Replacement={item.Replacement}, " +
        $"Reason={item.Reason}");
}
```

### API の使い分け

| API | 用途 | 文字列入力で許可する内容 |
|---|---|---|
| `Kanji.GetAlternatives` | MJ縮退マップにある候補を一覧取得。オプション指定時は登録済みIVS/SVSの基底文字も候補に追加 | 漢字一文字、または登録済み IVS/SVS |
| `MjFallback.GetSources`（mainブランチで利用可能・公開済みNuGet 1.2.1には未収録） | 指定文字を縮退先とするMJ縮退元表現と、その関係の根拠を取得する | `KanjiCharacter.Parse` が認識する一文字（漢字やMJデータに登録された `〆` など）、または登録済みIVS/SVS（VS付き入力の結果は空） |
| `Kanji.TryGetAlternative` | MJ縮退マップの一意な変換先を優先。使用できない場合、オプション指定時は登録済みIVS/SVSの基底文字を取得 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.IsSupported` | 漢字表現そのものが指定された `CharacterSet` に含まれるか確認 | 漢字一文字、または登録済み IVS/SVS |
| `KanjiText.Replace` | 変換可能な漢字を文字列内で置換。オプション指定時は登録済みIVS/SVSの基底文字へフォールバック | 任意の文字列 |
| `KanjiText.IsSupported` | 文字列全体を指定された `CharacterSet` で検証 | 任意の文字列 |
| `KanjiDiagnostics.Analyze` | 一つの漢字表現についてUnicode・JIS・常用/教育/人名用・音訓配当・MJ情報をまとめて取得 | 漢字一文字、または登録済みIVS/SVS |
| `KanjiDiagnostics.AnalyzeText` | 文字列全体について文字集合への適合性・置換先・判定理由・UTF-16位置を取得 | 任意の文字列 |
| `MjCharacter` | MJ文字図形名・Unicode・IVS/SVS・JIS X 0213・漢字施策等のMJ文字情報を参照 | MJ文字図形名、Unicodeスカラー一文字、または登録済みIVS/SVS |
| `JoyoKanji` | 常用漢字表の本表字、旧字体等、音訓、語例、備考を参照 | 常用漢字表の本表字一文字 |
| `JoyoKanjiDiagnostics` | 文章中の漢字が常用漢字表の本表字かを診断し、本表字でない漢字表現・不正UTF-16を位置付きで取得 | 任意の文字列 |
| `JoyoReading` | 本表字と読みの組み合わせが常用漢字表に掲載されているか、表外読みかを判定 | 本表字一文字と読み。読みの正誤判定はしない |
| `JinmeiyoKanji` | 人名用漢字863字の判定と、常用漢字を含めた名前に使用可能な漢字の判定 | 漢字一文字。IVS/SVS付き表現は対象外 |
| `EducationKanji` | 小学校の学年別漢字配当表を参照し、配当学年を取得 | 配当表に掲載された漢字一文字 |
| `EducationKanjiDiagnostics` | 指定小学学年までに習う漢字で文章が構成されているかを位置付きで診断 | 任意の文字列（不正UTF-16も診断項目として保持） |
| `JoyoKanjiEducation` | 常用漢字の各音訓について、小学校・中学校・高等学校の指導段階を参照 | 常用漢字表の本表字一文字 |
| `JoyoKanjiAppendix` | 常用漢字表の付表語や都道府県名の特別な読みを、語単位で検索 | 語または読み |
| `Okurigana` | 文化庁「送り仮名の付け方」の公式掲載語・本則・例外・許容・付表を参照 | 公式掲載語または同じ項目の許容表記。未掲載語の正誤判定はしない |

`KanjiCharacter.Parse` を使用するAPIの「漢字一文字または登録済みIVS/SVS」という入力説明には、同パーサーが認識するMJデータ上の文字（`〆`など）も含まれます。

`MjFallback.GetSources` は縮退先から元表現を逆引きするAPIです。mainブランチで利用できますが、公開済みNuGet 1.2.1には含まれていません。`Kanji.GetAlternatives` の候補とは検索方向と用途が異なります。

`GetAlternatives` は候補一覧、`TryGetAlternative` は一意変換表を優先した結果です。候補一覧が1件でも、それだけで一意な変換先とは判断しません。`KanjiCharacter.Parse` は形式が不正なら `FormatException`、`TryParse` は `false` を返します。

常用漢字関連では、`JoyoKanji` が漢字と音訓そのもの、`EducationKanji` が小学校での漢字単位の配当学年、`JoyoKanjiEducation` が音訓単位の学校段階、`JoyoKanjiAppendix` が付表語・都道府県名の読みを担当します。`JinmeiyoKanji` は人名用漢字863字を参照し、常用漢字との和集合も判定します。

### 一意な変換先の一覧

MJ縮退マップの一意な変換表から作成した一覧を [`data/MJ/MJUniqueAlternatives.1.2.0.csv`](data/MJ/MJUniqueAlternatives.1.2.0.csv) としてリポジトリで公開しています。
同じUnicode表現が複数行に現れる場合があります。MJ文字図形名で区別される字形ごとに変換先が異なることがあるためです。この一覧は特定の文字集合への収録可否で絞り込んでいません。変換先のUCSと、データに記録がある場合はJIS X 0213の面区点位置を掲載しています。

### 文字列処理のルール

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

### APIドキュメント

公開ドキュメント: [https://koichi-kobayashi.github.io/KanjiVariants/](https://koichi-kobayashi.github.io/KanjiVariants/)

DocfxのLocal Toolで、XMLコメントからAPIリファレンスと利用ガイドを生成できます。

```powershell
dotnet tool restore
dotnet docfx docs/docfx.json
```

HTMLの出力先は `docs/_site/` です。`dotnet docfx serve docs/_site --port 8080` でローカル閲覧できます。詳細は[生成・閲覧方法](https://koichi-kobayashi.github.io/KanjiVariants/articles/building-docs.html)、入口は[ドキュメントトップ](https://koichi-kobayashi.github.io/KanjiVariants/)を参照してください。

## ビルドと検証

```powershell
dotnet build KanjiVariants.slnx -c Release
dotnet test KanjiVariants.Tests/KanjiVariants.Tests.csproj -c Release
```

ユニットテストは、生成済みデータとライブラリのAPI・検索結果の整合性を検証します。ただし、原典から固定データへの転記が全件正しいことを、ユニットテストだけで保証するものではありません。

KanjiVariantsでは、ユニットテストに加えて、Generatorのテスト、生成済みファイルとGenerator出力の一致確認、および代表的な原典値を固定したテストを組み合わせて検証しています。これらのGenerator検証はGitHub Actionsでも実行します。

ユニットテストにはxUnitを使用しています（`xunit.v3.mtp-off` 4.0.0）。MJ等の生成データを更新するには、Python 3 で `python tools/generate.py` を実行します。常用漢字データは固定した `data/Joyo/JoyoKanjiOnkunIndex.html` から `python tools/generate_joyo.py` で再生成できます。原典の取得を更新する場合だけ `python tools/fetch_joyo.py` を明示的に実行します。実行時のネットワーク接続は不要です。

MJ文字情報API用データは固定した `data/MJ/mji.00602.xlsx` から `python tools/generate_mj_character.py` で再生成し、`python tools/generate_mj_character.py --check` で生成済みデータとの一致を確認できます。実行時にExcelやネットワーク接続は必要ありません。

人名用漢字データは固定した `data/MJ/mji.00602.xlsx` から `python tools/generate_jinmeiyo.py` で再生成し、`python tools/generate_jinmeiyo.py --check` で生成結果との一致を確認できます。

送り仮名データは `data/Okurigana/` に固定した文化庁公式HTML（`rule1.html`～`rule7.html`、`appendix.html`）から `python tools/generate_okurigana.py` で再生成し、`python tools/generate_okurigana.py --check` で生成済みファイルとの一致を確認できます。原典HTMLの更新取得は `python tools/fetch_okurigana.py` を明示的に実行します。通常の生成・ビルド・テストで取得処理は実行しません。Generatorは固定HTMLのSHA-256、見出し、語例ブロック数、通則別・区分別件数を検証し、変化があれば停止します。原典更新後は解析方法と掲載語を再照合してください。

教育漢字データは `python tools/generate_education_kanji.py` で再生成し、`--check` で生成済みファイルとの一致を確認できます。平成29年告示版の原典PDFは画像の表なので、その転記を `data/Education/EducationKanjiGradeTable.txt` に固定しています。Generatorは固定PDFのSHA-256と転記の件数・重複を検証します。原典の更新取得は `python tools/fetch_education_kanji.py` で明示的に行い、PDFが変わった場合は転記を再照合してください。

性能計測は別プロジェクトで実行します。`dotnet run --project KanjiVariants.Benchmarks/KanjiVariants.Benchmarks.csproj -c Release -- --filter "*"` はBenchmarkDotNetを復元し、検索・置換とメモリ割り当てを計測します。

音訓別の学校段階と付表は、固定した `data/Joyo/JoyoKanjiSchoolStages2017.pdf` から `python tools/generate_joyo_education.py` で再生成できます。座標付きPDF文字抽出に `pdfplumber` が必要です。OCRは使用しません。`--check` で生成済みファイルとの一致を確認できます。原PDFのテキスト層で「𠮟」だけが欠落するため、Generatorは該当行の位置・音訓と既存 `JoyoKanji` を検証してから補正します。

JIS X 0212データは固定した `data/JIS/jisx-212.ucm` と `data/Unicode/Unihan_OtherMappings.18.0.0.txt` から `python tools/generate_jis_x0212.py` で再生成し、`--check` で一致を確認できます。通常の生成・ビルドはネットワークへアクセスしません。取得だけを更新する場合は `python tools/fetch_jis_x0212.py` を明示的に実行します。固定原典のSHA-256、全件数、Unicode scalar・区点の妥当性と一意性、漢字部分の一致を検証します。

## 配布前のビルド・パス検証

全構成に決定論的SourceRoot/PathMap/SourceLink正規化を適用し、PDBを削除せずソース位置を仮想化します。公開著作権者名・SourceLinkのHTTPS URLはローカルパスと区別します。正式パッケージのソースcommit・通知・版の確認は別途必要です。

```powershell
dotnet run --project tools/VerifyBuildArtifacts -c Release -- KanjiVariants/bin/Release/net6.0/KanjiVariants.dll KanjiVariants/bin/Release/net6.0/KanjiVariants.pdb
```

CIでも同じ検査器を使います。ローカルpackはGit管理外の出力先を指定し、公開済みパッケージを置換・公開しないでください。

## データと出典

JIS X 0208は `data/JIS/JIS0208.TXT`（SHA-256 `1c571870457f19c97720631fa83ee491549a96ba1436da1296786a67d8632e87`）をオフラインで読みます。原典hash・全8,836区点・割当6,879・未割当1,957と1区32点の互換方針を検証し、Python codecの版に依存しません。[監査記録](docs/JIS-X0208-PROVENANCE-AUDIT.md)を参照してください。

MJ一覧 Ver.006.02/縮退マップ Ver.1.2.0の原著作者はIPA、加工者はKoichi Kobayashi。検索/所属/漢字施策/文字情報/代替候補・一意変換/縮退関係の逆引き索引の加工データ（生成C#の該当表・派生CSV・DLL内の同データ）はCC BY-SA 2.1 JPです。独自C#コードMITと分け、他の外部データは各条件を保持します。独立アプリケーションコード全体のライセンスを変更するものではありません。対象範囲と加工は [LICENSE-NOTICES.md](LICENSE-NOTICES.md#mj加工データのライセンス範囲) に記載しています。

逆引き索引（`GeneratedMjFallbackData.g.cs`）は、既存のMJ文字情報一覧表 Ver.006.02とMJ縮退マップ Ver.1.2.0から直接関係と根拠を再構成したものです。新しい外部データは追加せず、DLL内の同索引にもMJ加工データのCC BY-SA 2.1 JPの条件を維持します。登録済みシーケンスの照合に使う既存Unicodeデータには、同通知に記載されたUnicodeの条件が適用されます。

生成元データを `data/` に固定し、実行時には生成済みデータを使います。JIS X 0213の所属情報は、MJ文字情報一覧表 Ver.006.02の「実装したUCS」と「X0213」に基づいています。

| データ | バージョン・出典 |
|---|---|
| MJ文字情報一覧表 | Ver.006.02、[文字情報技術促進協議会](https://moji.or.jp/mojikiban/mjlist/) |
| MJ縮退マップ、MJ縮退マップ 一意な変換表 | Ver.1.2.0、[文字情報技術促進協議会](https://moji.or.jp/mojikiban/map/) |
| Unicode IVD | 2026-08-03、[Unicode IVD](https://www.unicode.org/ivd/data/2026-08-03/) |
| Unicode Standardized Variants | Unicode 18.0.0、[Unicode Character Database](https://www.unicode.org/Public/18.0.0/ucd/StandardizedVariants.txt) |
| JIS X 0212のUnicode対応 | [Unicode ICU対応表](https://github.com/unicode-org/icu/blob/61607c27732906d36c5bd4d23ecc092f89f53a2b/icu4c/source/data/mappings/jisx-212.ucm)、Table version 0.9（2000-08-20）、コミット固定。漢字部分は[Unihan 18.0.0](https://www.unicode.org/Public/18.0.0/ucd/Unihan.zip)の `kJis1` と照合。[公式プロパティ仕様](https://www.unicode.org/reports/tr38/#kJis1)はJIS X 0212-1990の区点を定義。 |
| JIS X 0208 の Unicode 対応 | 固定Unicode JIS0208.TXT Table 2.0（2011-10-14、header更新2015-12-02）、Unicode License V3。1区32点をU+FF3Cへ変更し従来互換。legacy mappingの一例。CP932拡張は含めない。 |
| 常用漢字表の音訓索引 | [文化庁「常用漢字表の音訓索引」](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/kanji/joyokanjisakuin/index.html)。公開HTMLを固定し、本表の文字・音訓・語例・備考をKanjiVariants用に抽出・再構成。 |
| 学年別漢字配当表 | [文部科学省「小学校学習指導要領（平成29年告示）」別表「学年別漢字配当表」](https://www.mext.go.jp/content/20230120-mxt_kyoiku02-100002604_01.pdf)。公式PDFを固定し、画像表を転記して検索表へ加工。各学年80・160・200・202・193・191字。 |
| 音訓の小・中・高等学校段階別割り振り表 | 文部科学省、平成29年3月。[案内ページ](https://www.mext.go.jp/a_menu/shotou/new-cs/1385768.htm)・[原典PDF](https://www.mext.go.jp/a_menu/shotou/new-cs/__icsFiles/afieldfile/2017/05/15/1385768.pdf)。固定PDFの文字と座標から本表の音訓別段階・1字下げ、付表1・付表2を抽出。 |
| 送り仮名の付け方 | [文化庁の公式本文](https://www.bunka.go.jp/kokugo_nihongo/sisaku/joho/joho/kijun/naikaku/okurikana/index.html)。2026-09-30取得の通則1～7（`honbun01.html`～`honbun07.html`）と付表（`huhyo.html`）を固定し、掲載語、許容表記、注記を抽出・再構成。各ページの取得元URLは `tools/fetch_okurigana.py` に記載。 |

### 学年別漢字配当表の照合結果

2026-09-29に、`data/Education/EducationKanjiGradeTable.txt` の**収録文字と所属学年**を、文部科学省の公開資料から独立に再構成して照合しました。照合対象の平成29年版PDFのSHA-256は `6AF90F134B243E44F9767C37EE3079FAC092883FD6359B836A5733DD25B43902` で、TXTに記録した値と一致しています。

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

[`data/Unicode/Japanese_IVS_2026-08-03.xlsx`](data/Unicode/Japanese_IVS_2026-08-03.xlsx) は、[Unicode IVD 2026-08-03](https://www.unicode.org/ivd/data/2026-08-03/IVD_Sequences.txt) からMoji_Joho、Adobe-Japan1、Hanyo-DenshiのCollectionを抽出した閲覧用Excelファイルです。`BaseCharacter` 列と `IVSCharacter` 列には、[IPAmj明朝](https://moji.or.jp/mojikiban/font/) を指定しています。この一覧は上記のランタイム用生成データとは別の参考資料であり、一覧への掲載だけでライブラリの対応範囲が広がるわけではありません。

IPAmj明朝の配布元は、同フォントのIVS実装が2017-12-12版のMoji_Johoコレクションに準拠すると説明しています。そのため、Adobe-Japan1やHanyo-Denshiのシーケンス、または後のIVDで追加されたシーケンスでは、Excel上で指定された字形が表示されるとは限りません。見た目が同じでもVariation Selectorがないとは判断せず、符号位置を確認してください。

MJデータの著作権者は独立行政法人情報処理推進機構（IPA）です。MJ文字情報一覧表とMJ縮退マップは [CC BY-SA 2.1 JP](https://creativecommons.org/licenses/by-sa/2.1/jp/) により提供されています。Unicodeのデータは [Unicode Terms of Use](https://www.unicode.org/terms_of_use.html) に従います。文化庁の公開情報は[文部科学省ウェブサイト利用規約](https://www.mext.go.jp/b_menu/1351168.htm)を参照してください。出典と加工の詳細は [LICENSE-NOTICES.md](LICENSE-NOTICES.md) に記載しています。

## 免責事項

本ライブラリおよび同梱データは現状のまま提供されます。本ライブラリまたは同梱データの利用により発生したいかなる損害についても、作者は一切責任を負いません。ライセンス条件の異なる同梱データについては、[LICENSE-NOTICES.md](LICENSE-NOTICES.md) もご確認ください。
