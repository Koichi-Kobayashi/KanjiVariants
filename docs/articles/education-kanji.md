# 教育漢字

`EducationKanji` は、文部科学省「小学校学習指導要領（平成29年告示）」の学年別漢字配当表1,026字を参照します。`KanjiGrade.Grade1`～`Grade6` は小学校の配当学年です。

```csharp
using KanjiVariants;

bool included = EducationKanji.IsEducationKanji("学"); // true
KanjiGrade? grade = EducationKanji.GetGrade("学"); // Grade1
var firstGrade = EducationKanji.GetByGrade(KanjiGrade.Grade1); // 80字
```

各学年は80・160・200・202・193・191字です。対象外の文字の `GetGrade` はnullです。`GetByGrade` は転記データ順の共有読み取り専用一覧で、原典の掲載順との厳密な一致は前提にしないでください。

配当表に載る文字そのものだけを判定します。旧字体・異体字・IVS/SVSの自動変換は行いません。漢字単位の配当学年は、[音訓単位の学校段階](school-stages.md)とは別の情報です。

関連API: <xref:KanjiVariants.EducationKanji>・<xref:KanjiVariants.KanjiGrade>
