# 常用漢字

`JoyoKanji` は文化庁「常用漢字表の音訓索引」の本表字2,136字を参照します。`JoyoKanjiEntry` は本表字、旧字体等、音訓を保持し、`KanjiReading` は読み・音訓種別・語例・備考を保持します。`KanjiReadingType.On` / `Kun` は音読み／訓読みです。

```csharp
using KanjiVariants;

var entry = JoyoKanji.Get("亜");
var oldForm = entry?.OldForm; // 亞
var matches = JoyoKanji.FindByReading("こう");
var onOnly = JoyoKanji.FindByReading("こう", KanjiReadingType.On);
```

`OldForm` は併記が1字の場合だけ設定されます。「弁」のように複数ある場合はnullで、`OldForms` に全件を保持します。`SourceLabel` は角括弧の字形注記を含む原典の文字欄です。

読みの検索はNFC正規化後にひらがな・カタカナの差を吸収した完全一致で、返す読みは原典表記です。備考は字単位の情報を各読みへ保持し、特定の音訓への適用を推定しません。

旧字体・異体字・IVS/SVSを本表字へ自動変換しません。例えば「亞」「髙」そのものは常用漢字と判定しません。必要な変換は利用者が明示的に組み合わせます。

[音訓の学校段階と付表](school-stages.md)・[表外読み](joyo-reading.md)

関連API: <xref:KanjiVariants.JoyoKanji>・<xref:KanjiVariants.JoyoKanjiEntry>・<xref:KanjiVariants.KanjiReading>・<xref:KanjiVariants.KanjiReadingType>
