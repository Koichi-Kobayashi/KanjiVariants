// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using KanjiVariants;

// 10個の独立プロセスで各1回。pilot/warmupを行わず、API初回呼び出し(JIT込み)を測ります。
// OSのプロセス起動・ランタイム起動は計時外です。
// MemoryDiagnoserは同じプロセスで再実行してwarm値を測るため、ここでは付けません。
// 初回のAllocated/GC/保持メモリは --mj-probe により別の新規プロセスで取得します。
[SimpleJob(RunStrategy.ColdStart, launchCount: 10, warmupCount: 0, iterationCount: 1, invocationCount: 1)]
[MedianColumn]
public class MjInitializationBenchmarks
{
    private int _calls;

    [Benchmark] public MjCharacterEntry? NameFirst() { _calls++; return MjCharacter.GetByMjGlyphName(MjBaselineFixtures.NameStart); }
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindBmpFirst() { _calls++; return MjCharacter.Find(MjBaselineFixtures.Bmp); }
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindSupplementaryFirst() { _calls++; return MjCharacter.Find(MjBaselineFixtures.Supplementary); }
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindIvsFirst() { _calls++; return MjCharacter.Find(MjBaselineFixtures.Ivs); }
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindSvsFirst() { _calls++; return MjCharacter.Find(MjBaselineFixtures.Svs); }
    [Benchmark] public IReadOnlyList<MjCharacterMatch> FindMissFirst() { _calls++; return MjCharacter.Find(MjBaselineFixtures.Miss); }

    [GlobalCleanup]
    public void VerifySingleInvocation()
    {
        // 測定終了後に確認し、誤ってwarmup/診断再実行を追加した場合を検出します。
        MjBaselineFixtures.Require(_calls == 1, "cold process must invoke exactly once");
        MjBaselineFixtures.Validate();
    }
}
