// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using KanjiVariants;

// 解析済みの文字を使い、配当表検索そのものを計測します。
[MemoryDiagnoser]
public class EducationKanjiBenchmarks
{
    private KanjiCharacter _hit;
    private KanjiCharacter _miss;

    [GlobalSetup]
    public void Setup()
    {
        _hit = KanjiCharacter.Parse("学");
        _miss = KanjiCharacter.Parse("髙");
    }

    [Benchmark]
    public bool IsEducationKanji_Hit() => EducationKanji.IsEducationKanji(_hit);

    [Benchmark]
    public bool IsEducationKanji_Miss() => EducationKanji.IsEducationKanji(_miss);

    [Benchmark]
    public KanjiGrade? GetGrade_Hit() => EducationKanji.GetGrade(_hit);

    [Benchmark]
    public KanjiGrade? GetGrade_Miss() => EducationKanji.GetGrade(_miss);

    [Benchmark]
    public IReadOnlyList<KanjiCharacter> GetByGrade_Grade1() => EducationKanji.GetByGrade(KanjiGrade.Grade1);

    [Benchmark]
    public IReadOnlyList<KanjiCharacter> GetByGrade_Grade6() => EducationKanji.GetByGrade(KanjiGrade.Grade6);
}
