# 文字集合とフォールバック

| CharacterSet | 対象 |
|---|---|
| `JisX0208` | JIS X 0208全体。非漢字・第1水準漢字・第2水準漢字を含む |
| `JisX0213Plane1` | JIS X 0213第1面 |
| `JisX0213Plane2` | JIS X 0213第2面 |
| `JisX0213` | JIS X 0213第1面と第2面の和集合 |

第1水準・第2水準はJIS X 0208内の分類です。現時点では水準ごとに指定するAPIはありません。JIS X 0213の第1面・第2面とは異なる分類です。

CP932（Windows-31J）はJIS X 0208を基礎にした符号化方式で、独自拡張も含みます。`JisX0208` はCP932の独自拡張やJIS X 0213追加文字を含めません。本ライブラリはShift-JISのエンコーダー／デコーダーではなく、JIS X 0201を含むShift-JIS全体の文字検査も行いません。

## IVS/SVS

登録済みの基底文字＋Variation Selectorは一つの漢字表現として解析します。ただし、そのシーケンス自体は上記のどの文字集合でもsupportedにはなりません。

`KanjiFallbackOptions.None` はVSを削除しません。`AllowVariationSelectorFallback` は、登録済みシーケンスの基底文字が指定集合で使用可能な場合だけ、その基底文字へフォールバックできます。MJ一意変換表の変換先が使用可能な場合はそちらを優先します。未登録のシーケンスはVS fallback対象外です。VSの除去では字形指定が失われます。

```csharp
using KanjiVariants;

bool plane1 = Kanji.IsSupported("丑", CharacterSet.JisX0213Plane1); // true
bool plane2 = Kanji.IsSupported("丑", CharacterSet.JisX0213Plane2); // false
var result = KanjiText.Replace("辻\U000E0100", CharacterSet.JisX0208,
    KanjiFallbackOptions.AllowVariationSelectorFallback); // 辻
```

`IsSupported` は入力そのものを検証し、fallbackは行いません。`Replace` は置換できない文字を維持するため、必要に応じて出力を別途検証してください。

関連API: <xref:KanjiVariants.CharacterSet>・<xref:KanjiVariants.Kanji>・<xref:KanjiVariants.KanjiText>・<xref:KanjiVariants.KanjiCharacter>・<xref:KanjiVariants.KanjiFallbackOptions>
