# Getting Started

## 導入

.NET 6以降のプロジェクトにNuGetパッケージを追加します。

```powershell
dotnet add package KanjiVariants
```

C#では `using KanjiVariants;` を指定します。以下は既存APIを使う基本例です。

```csharp
using KanjiVariants;

// 使用可能な代替候補。候補数だけで一意な変換先とは判断しません。
var candidates = Kanji.GetAlternatives("髙", CharacterSet.JisX0208);

// MJ一意変換表に定義された代替文字が使用可能な場合に置換します。
var replaced = KanjiText.Replace("髙・𠮷", CharacterSet.JisX0208);

var joyo = JoyoKanji.Get("亜");
var readings = JoyoKanji.GetReadings("高");
bool jinmeiyo = JinmeiyoKanji.IsJinmeiyoKanji("丑");
var okurigana = Okurigana.Find("行う");
var mj = MjCharacter.Find("髙");
```

代替できない文字は `Replace` 後も残ります。出力全体を検証するには `KanjiText.IsSupported(replaced, CharacterSet.JisX0208)` を使います。

IVS/SVSから基底文字へ落とす場合は、字形指定が失われることを理解した上で明示的に許可します。

```csharp
var baseText = KanjiText.Replace("辻\U000E0100", CharacterSet.JisX0208,
    KanjiFallbackOptions.AllowVariationSelectorFallback);
```

[文字集合とフォールバック](character-sets.md)・[機能別API一覧](../api/index.md)も参照してください。
