# 表外読み

`JoyoReading` は常用漢字表の本表字と読みの組み合わせを判定します。

| API | trueとなる条件 |
|---|---|
| `IsListedReading` | 漢字が本表字で、読みが掲載音訓に一致する |
| `IsHyogaiReading` | 漢字が本表字で、読みが掲載音訓に一致しない |

```csharp
using KanjiVariants;

bool listed = JoyoReading.IsListedReading("高", "こう"); // true
bool hyogai = JoyoReading.IsHyogaiReading("愛", "いとしい"); // true
```

表外読みは「常用漢字表に掲載されていない読み」であり、誤りという意味ではありません。入力が実際に使われる読みかどうかも判定しません。

漢字自体が本表字でなければ両APIともfalseです。旧字体・異体字・IVS/SVSを自動変換しません。読みはNFC正規化とひらがな・カタカナの差を吸収して完全一致で比較します。活用形展開・送り仮名補正・部分一致・類推は行いません。nullの読みは例外、空の読みは未掲載として扱います。

関連API: <xref:KanjiVariants.JoyoReading>・<xref:KanjiVariants.JoyoKanji>
