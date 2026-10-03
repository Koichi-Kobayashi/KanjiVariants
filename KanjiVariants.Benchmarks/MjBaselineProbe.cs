// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using KanjiVariants;

internal static class MjBaselineProbe
{
    // 使用例: dotnet run --project KanjiVariants.Benchmarks -c Release -- --mj-probe NameFirst
    // 各ケース・反復ごとに新しいプロセスで起動してください。出力はJSONです。
    internal static bool TryRun(string[] args)
    {
        if (args.Length == 1 && args[0] == "--mj-validate")
        {
            MjBaselineFixtures.Validate();
            Console.WriteLine(JsonSerializer.Serialize(MjBaselineFixtures.Inputs.Select(input => new
            {
                Input = input,
                Matches = MjCharacter.Find(input).Select(m => new { m.Entry.MjGlyphName, Kind = m.MatchKind.ToString() })
            })));
            return true;
        }
        if (args.Length != 2 || args[0] != "--mj-probe") return false;
        string name = args[1];
        if (!new[] { "NameFirst", "FindBmpFirst", "FindSupplementaryFirst", "FindIvsFirst", "FindSvsFirst", "FindMissFirst" }.Contains(name))
            throw new ArgumentException("Unknown MJ probe: " + name);

        using var process = Process.GetCurrentProcess();
        // 計測API自体を準備し、初回MJ呼び出しの前にだけ強制GCを行います。
        _ = GC.GetAllocatedBytesForCurrentThread();
        _ = GC.GetTotalAllocatedBytes(true);
        _ = GC.GetGCMemoryInfo();
        _ = Stopwatch.GetTimestamp();
        var before = Snapshot(process);
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        long totalAllocated = GC.GetTotalAllocatedBytes(true);
        int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
        long start = Stopwatch.GetTimestamp();
        object result = Invoke(name);
        long elapsed = Stopwatch.GetTimestamp() - start;
        long allocatedDelta = GC.GetAllocatedBytesForCurrentThread() - allocated;
        long totalDelta = GC.GetTotalAllocatedBytes(true) - totalAllocated;
        int gc0 = GC.CollectionCount(0) - gen0, gc1 = GC.CollectionCount(1) - gen1, gc2 = GC.CollectionCount(2) - gen2;
        // 強制GC後の差は生存managedメモリの目安。GC回数/総割当にはこのGCを含めません。
        var after = Snapshot(process);
        GC.KeepAlive(result);
        MjBaselineFixtures.Validate();
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            Case = name, ProcessId = Environment.ProcessId, Runtime = Environment.Version.ToString(),
            DebuggerAttached = Debugger.IsAttached,
            ApiMilliseconds = elapsed * 1000.0 / Stopwatch.Frequency,
            AllocatedBytes = allocatedDelta, TotalAllocatedBytes = totalDelta,
            Gen0 = gc0, Gen1 = gc1, Gen2 = gc2,
            Before = before, After = after,
            ManagedRetainedDelta = after.Managed - before.Managed,
            PrivateBytesDelta = after.PrivateBytes - before.PrivateBytes,
            WorkingSetDelta = after.WorkingSet - before.WorkingSet
        }));
        return true;
    }

    // JITが呼び出し元へ展開してMJ型を早期初期化しないよう、呼び出し境界を固定します。
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Invoke(string name) => name switch
    {
        "NameFirst" => MjCharacter.GetByMjGlyphName(MjBaselineFixtures.NameStart)!,
        "FindBmpFirst" => MjCharacter.Find(MjBaselineFixtures.Bmp),
        "FindSupplementaryFirst" => MjCharacter.Find(MjBaselineFixtures.Supplementary),
        "FindIvsFirst" => MjCharacter.Find(MjBaselineFixtures.Ivs),
        "FindSvsFirst" => MjCharacter.Find(MjBaselineFixtures.Svs),
        "FindMissFirst" => MjCharacter.Find(MjBaselineFixtures.Miss),
        _ => throw new ArgumentException(name)
    };

    private static MemorySnapshot Snapshot(Process process)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long managed = GC.GetTotalMemory(true);
        var info = GC.GetGCMemoryInfo();
        process.Refresh();
        return new MemorySnapshot(managed, info.HeapSizeBytes, info.FragmentedBytes,
            process.PrivateMemorySize64, process.WorkingSet64);
    }

    private sealed record MemorySnapshot(long Managed, long HeapSize, long Fragmented, long PrivateBytes, long WorkingSet);
}
