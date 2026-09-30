# 送り仮名

`Okurigana` は文化庁「送り仮名の付け方」の公式本文に掲載された語を参照します。任意の日本語の正誤を判定したり、未掲載語へ規則を自動適用したりする機能ではありません。

```csharp
using KanjiVariants;

var entries = Okurigana.Find("行う");
var alternatives = Okurigana.Find("行なう"); // 同じ掲載項目
var rule1 = Okurigana.GetByRule(1);
var permitted = Okurigana.GetByKind(OkuriganaRuleKind.Permitted);
```

`OkuriganaEntry.Word` は掲載語、`AlternativeForms` は同じ項目の許容表記の共有一覧です。`Note` に条件・説明・読み・構成関係などの原典注記を保持します。

| OkuriganaRuleKind | 原典での区分 |
|---|---|
| `Principle` | 本則（注意に掲載された本則の語例を含む） |
| `Exception` | 例外（通則7もこの区分） |
| `Permitted` | 許容 |
| `Appendix` | 付表。RuleNumberはnull |

`Find` は掲載語と許容表記をNFC正規化後に完全一致検索し、該当なしは空一覧です。同じ語が複数の通則・区分にある場合は複数項目を返します。`GetByRule` は1～7、`GetByKind` は指定区分の項目を取得します。活用形展開・形態素解析は行いません。

関連API: <xref:KanjiVariants.Okurigana>・<xref:KanjiVariants.OkuriganaEntry>・<xref:KanjiVariants.OkuriganaRuleKind>
