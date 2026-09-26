// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KanjiVariants;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class LookupBenchmarks
{
    private ulong[] _scalarKeys = Array.Empty<ulong>();
    private int[] _scalarEntries = Array.Empty<int>();
    private ulong _variationKey;

    [Params(0x9AD9, 0x20BB7)]
    public int CodePoint { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // 直接表と二分探索で同じ EntryIndex が返るよう、比較用キーと値を作ります。
        var scalarEntries = Enumerable.Range(0, GeneratedData.BmpEntryIndices.Length)
            .Select(cp => (CodePoint: cp, Entry: GeneratedData.BmpEntryIndices[cp]))
            .Concat(Enumerable.Range(0, GeneratedData.SupplementaryEntryIndices.Length)
                .Select(i => (CodePoint: i + 0x20000, Entry: GeneratedData.SupplementaryEntryIndices[i])))
            .Where(item => item.Entry >= 0)
            .ToArray();
        _scalarKeys = scalarEntries.Select(item => (ulong)item.CodePoint).ToArray();
        _scalarEntries = scalarEntries.Select(item => item.Entry).ToArray();
        _variationKey = GeneratedData.VariationKeys[GeneratedData.VariationKeys.Length / 2];

        int direct = CodePoint < 0x10000
            ? GeneratedData.BmpEntryIndices[CodePoint]
            : GeneratedData.SupplementaryEntryIndices[CodePoint - 0x20000];
        int scalarIndex = Array.BinarySearch(_scalarKeys, (ulong)CodePoint);
        int binary = scalarIndex >= 0 ? _scalarEntries[scalarIndex] : -1;
        if (direct != binary)
            throw new InvalidOperationException("Scalar lookup implementations returned different entries.");
    }

    [Benchmark(Baseline = true)]
    public int DirectScalarLookup() => CodePoint < 0x10000
        ? GeneratedData.BmpEntryIndices[CodePoint]
        : GeneratedData.SupplementaryEntryIndices[CodePoint - 0x20000];

    [Benchmark]
    public int BinaryScalarLookup()
    {
        int index = Array.BinarySearch(_scalarKeys, (ulong)CodePoint);
        return index >= 0 ? _scalarEntries[index] : -1;
    }

    [Benchmark]
    public int VariationLookup()
    {
        // IVS/SVS と同じく、登録済み複合キーを二分探索して EntryIndex まで取得します。
        int index = Array.BinarySearch(GeneratedData.VariationKeys, _variationKey);
        return index >= 0 ? GeneratedData.VariationEntryIndices[index] : -1;
    }
}

[MemoryDiagnoser]
public class ReplaceBenchmarks
{
    [Params("ASCII only", "No replacement", "Mixed ASCII and kanji", "One replacement",
        "Many replacements", "Supplementary", "IVS", "SVS")]
    public string Scenario { get; set; } = "ASCII only";

    private string _text = "";

    // 測定中に入力データを組み立てず、各シナリオの置換処理だけを測ります。
    [GlobalSetup]
    public void Setup() => _text = Scenario switch
    {
        "ASCII only" => "ABC123456789",
        "No replacement" => "高橋吉野",
        "Mixed ASCII and kanji" => "ABC高橋-123",
        "One replacement" => "髙橋吉野",
        "Many replacements" => "髙橋𠮷野髙橋𠮷野",
        "Supplementary" => "𠮷野",
        "IVS" => "\u3404\U000E0101",
        "SVS" => "\u6B04\uFE00",
        _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
    };

    [Benchmark]
    public string Replace() => KanjiText.Replace(_text, CharacterSet.JisX0208);
}
