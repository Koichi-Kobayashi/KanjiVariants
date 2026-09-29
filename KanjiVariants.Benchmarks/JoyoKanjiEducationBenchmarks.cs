// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using KanjiVariants;

// 検索入力は初期化時に用意し、計測にはAPI呼び出しだけを含めます。
[MemoryDiagnoser]
public class JoyoKanjiEducationBenchmarks
{
    private KanjiCharacter _character;

    [GlobalSetup]
    public void Setup() => _character = KanjiCharacter.Parse("衣");

    [Benchmark]
    public IReadOnlyList<KanjiReadingEducation> GetReadings() =>
        JoyoKanjiEducation.GetReadings(_character);

    [Benchmark]
    public SchoolStage? GetReadingStage_Hit() =>
        JoyoKanjiEducation.GetReadingStage(_character, "ころも");

    [Benchmark]
    public SchoolStage? GetReadingStage_Miss() =>
        JoyoKanjiEducation.GetReadingStage(_character, "みとうろく");

    [Benchmark]
    public IReadOnlyList<KanjiReadingEducation> GetByStage() =>
        JoyoKanjiEducation.GetByStage(SchoolStage.JuniorHigh);

    [Benchmark]
    public IReadOnlyList<JoyoKanjiAppendixEntry> Appendix_FindByWord() =>
        JoyoKanjiAppendix.FindByWord("海女");

    [Benchmark]
    public IReadOnlyList<JoyoKanjiAppendixEntry> Appendix_FindByReading() =>
        JoyoKanjiAppendix.FindByReading("かぐら");
}
