// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using KanjiVariants;

[MemoryDiagnoser]
[MedianColumn]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 8)]
public class MjLookupBenchmarks
{
    // 初期化とfixture検証は測定外。返却値を利用して検索の除去を防ぎます。
    [GlobalSetup] public void Setup() => MjBaselineFixtures.Validate();

    [Benchmark] public MjCharacterEntry? NameStart() => MjCharacter.GetByMjGlyphName(MjBaselineFixtures.NameStart);
    [Benchmark] public MjCharacterEntry? NameMiddle() => MjCharacter.GetByMjGlyphName(MjBaselineFixtures.NameMiddle);
    [Benchmark] public MjCharacterEntry? NameEnd() => MjCharacter.GetByMjGlyphName(MjBaselineFixtures.NameEnd);
    [Benchmark] public MjCharacterEntry? NameMiss() => MjCharacter.GetByMjGlyphName(MjBaselineFixtures.NameMiss);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindImplemented() => MjCharacter.Find(MjBaselineFixtures.Implemented);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindCorresponding() => MjCharacter.Find(MjBaselineFixtures.Corresponding);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindIvs() => MjCharacter.Find(MjBaselineFixtures.Ivs);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindSvs() => MjCharacter.Find(MjBaselineFixtures.Svs);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindMultiple() => MjCharacter.Find(MjBaselineFixtures.Multiple);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindMiss() => MjCharacter.Find(MjBaselineFixtures.Miss);
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindMergedFlags() => MjCharacter.Find(MjBaselineFixtures.Bmp);
}
