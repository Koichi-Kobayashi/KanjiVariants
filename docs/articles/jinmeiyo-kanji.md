# 人名用漢字

MJ文字情報一覧表の「漢字施策 = 人名用漢字」に分類された863字の実装UCSを参照します。

| API | 判定・取得対象 |
|---|---|
| `IsJinmeiyoKanji` | 人名用漢字863字そのもの |
| `IsNameUsableKanji` | 常用漢字の本表字または人名用漢字 |
| `GetAll` | 人名用漢字863字の共有読み取り専用一覧 |

```csharp
using KanjiVariants;

bool jinmeiyo = JinmeiyoKanji.IsJinmeiyoKanji("丑"); // true
bool usable = JinmeiyoKanji.IsNameUsableKanji("高"); // true（常用漢字）
var all = JinmeiyoKanji.GetAll();
```

名前に使用可能な**漢字**の判定です。ひらがな・カタカナなど、名前に使える文字全般の判定ではありません。旧字体・異体字・IVS/SVSを自動変換せず、入力された文字そのものを判定します。

関連API: <xref:KanjiVariants.JinmeiyoKanji>
