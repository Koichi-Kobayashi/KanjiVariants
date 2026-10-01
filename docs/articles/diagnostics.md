# 文字診断

<xref:KanjiVariants.KanjiDiagnostics> の `Analyze` で、一つの漢字表現について既存APIの情報を集約した <xref:KanjiVariants.KanjiDiagnosticResult> を取得できます。

```csharp
using KanjiVariants;

var result = KanjiDiagnostics.Analyze("高");
Console.WriteLine($"U+{result.BaseCodePoint:X}");
Console.WriteLine(result.IsJisX0208);
Console.WriteLine(result.EducationGrade);
foreach (var match in result.MjMatches)
    Console.WriteLine($"{match.Entry.MjGlyphName}: {match.MatchKind}");
```

## 診断項目

| 分類 | プロパティと意味 |
|---|---|
| 元の表現 | `Character`。入力の漢字表現を保持 |
| Unicode | `BaseCodePoint`、`IsBaseCharacterBmp`、`HasVariationSelector`、`VariationSelectorCodePoint` |
| 表現の長さ | `UnicodeScalarCount`は通常1、IVS/SVSは2。`Utf16CodeUnitCount`はUTF-16コード単位数で、`Character.ToString().Length`相当 |
| JIS | `IsJisX0208`、`IsJisX0212`、`IsJisX0213Plane1`、`IsJisX0213Plane2`、両面の和集合を表す`IsJisX0213` |
| 常用漢字 | `IsJoyo`、`JoyoEntry`。常用漢字表の本表字でなければfalse／null |
| 教育漢字 | `IsEducationKanji`、`EducationGrade`。配当表外ならfalse／null |
| 人名用漢字 | `IsJinmeiyoKanji`は人名用漢字863字。`IsNameUsableKanji`は常用漢字または人名用漢字 |
| 音訓配当 | `ReadingEducation`。小学校・中学校・高等学校ごとの音訓配当を既存APIの読み取り専用一覧で返す。本表字以外は空 |
| MJ情報 | `MjMatches`。元の表現に一致したMJ文字情報とFlagsによる一致理由を既存APIの読み取り専用一覧で返す |

BMP内の漢字はUTF-16で1単位、BMP外の漢字は2単位です。BMP内漢字とBMP外のVariation Selectorの組み合わせは3単位になります。`IsBaseCharacterBmp`は基底文字だけの範囲を示します。

## IVS/SVSと入力制約

文字列は <xref:KanjiVariants.KanjiCharacter> の `Parse` で厳密に解析します。nullは `ArgumentNullException`、空文字・複数の論理文字・不正UTF-16・漢字以外・未登録Variation Sequenceは `FormatException` です。解析済みの `KanjiCharacter` は再解析せず診断します。

登録済みIVS/SVSは受理しますが、JIS所属や常用・教育・人名用の判定で基底文字へ自動縮退しません。MJ検索も元のシーケンスを検索し、基底文字の一致を追加しません。

```csharp
var ivs = KanjiDiagnostics.Analyze("辻\U000E0102");
Console.WriteLine(ivs.HasVariationSelector); // true
Console.WriteLine(ivs.IsJisX0208);           // false
Console.WriteLine(ivs.UnicodeScalarCount);   // 2
Console.WriteLine(ivs.Utf16CodeUnitCount);   // 3
```

一般文字、名前全体の使用可否、送り仮名、表外読み、代替候補は診断しません。文字集合別の代替候補には `Kanji.GetAlternatives` を使ってください。
