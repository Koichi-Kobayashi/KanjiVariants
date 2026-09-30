# 音訓の学校段階と付表

文部科学省「音訓の小・中・高等学校段階別割り振り表（平成29年3月）」を参照します。`JoyoKanjiEducation` は音訓ごとに `SchoolStage.Elementary`・`JuniorHigh`・`HighSchool` を返します。漢字単位の配当学年とは別の情報で、指導の目安です。

```csharp
using KanjiVariants;

var stage = JoyoKanjiEducation.GetReadingStage("衣", "ころも"); // JuniorHigh
var readings = JoyoKanjiEducation.GetReadings("宮");
var ama = JoyoKanjiAppendix.FindByWord("海女");
var kagura = JoyoKanjiAppendix.FindByReading("かぐら");
```

`KanjiReadingEducation.Reading` は既存の音訓を共有します。`IsSpecialOrLimited` は原典の1字下げによる特別なもの・用法の狭い音訓の指定です。

`JoyoKanjiAppendix` は付表1の語と付表2の都道府県名の読みを語単位で検索します。`JoyoKanjiAppendixEntry.Words` に異表記をまとめ、`JoyoKanjiAppendixKind` で区分します。

関連API: <xref:KanjiVariants.JoyoKanjiEducation>・<xref:KanjiVariants.KanjiReadingEducation>・<xref:KanjiVariants.SchoolStage>・<xref:KanjiVariants.JoyoKanjiAppendix>・<xref:KanjiVariants.JoyoKanjiAppendixEntry>・<xref:KanjiVariants.JoyoKanjiAppendixKind>
