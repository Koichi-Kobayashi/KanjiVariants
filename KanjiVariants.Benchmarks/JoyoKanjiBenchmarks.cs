// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using KanjiVariants;

// 検索用の入力は計測前に確定し、解析や文字列生成を測定区間に含めません。
[MemoryDiagnoser]
public class JoyoKanjiBenchmarks
{
    private KanjiCharacter _hit;
    private KanjiCharacter _miss;

    [GlobalSetup]
    public void Setup()
    {
        _hit = KanjiCharacter.Parse("亜");
        _miss = KanjiCharacter.Parse("髙");
    }

    [Benchmark]
    public bool IsJoyo_Hit() => JoyoKanji.IsJoyo(_hit);

    [Benchmark]
    public bool IsJoyo_Miss() => JoyoKanji.IsJoyo(_miss);

    [Benchmark]
    public JoyoKanjiEntry? Get_Hit() => JoyoKanji.Get(_hit);

    [Benchmark]
    public JoyoKanjiEntry? Get_Miss() => JoyoKanji.Get(_miss);

    [Benchmark]
    public IReadOnlyList<KanjiReading> GetReadings() => JoyoKanji.GetReadings(_hit);

    [Benchmark]
    public IReadOnlyList<JoyoKanjiEntry> FindByReading_On() => JoyoKanji.FindByReading("コウ", KanjiReadingType.On);

    [Benchmark]
    public IReadOnlyList<JoyoKanjiEntry> FindByReading_Kun() => JoyoKanji.FindByReading("あわれ", KanjiReadingType.Kun);

    [Benchmark]
    public bool IsReadingSupported_Hit() => JoyoKanji.IsReadingSupported(_hit, "ア");

    [Benchmark]
    public bool IsReadingSupported_Miss() => JoyoKanji.IsReadingSupported(_hit, "アイ");
}
