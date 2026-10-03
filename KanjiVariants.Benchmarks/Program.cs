// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using KanjiVariants;

if (!MjBaselineProbe.TryRun(args))
    BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

[MemoryDiagnoser]
public class LookupBenchmarks
{
    private ulong[] _scalarKeys = Array.Empty<ulong>();
    private int[] _scalarEntries = Array.Empty<int>();

    [Params(0x9AD9, 0x20BB7)]
    public int CodePoint { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // 比較用に、通常文字の登録コードポイントとEntry番号を昇順配列にします。
        var scalarEntries = Enumerable.Range(0, GeneratedData.BmpEntryIndices.Length)
            .Select(cp => (CodePoint: cp, Entry: GeneratedData.BmpEntryIndices[cp]))
            .Concat(Enumerable.Range(0, GeneratedData.SupplementaryEntryIndices.Length)
                .Select(i => (CodePoint: i + 0x20000, Entry: GeneratedData.SupplementaryEntryIndices[i])))
            .Where(item => item.Entry >= 0)
            .ToArray();
        _scalarKeys = scalarEntries.Select(item => (ulong)item.CodePoint).ToArray();
        _scalarEntries = scalarEntries.Select(item => item.Entry).ToArray();

        int direct = DirectScalarLookup();
        int binary = BinaryScalarLookup();
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
}

// Variation Sequence 検索に通常コードポイントのParamsは関係しないため、独立して測ります。
[MemoryDiagnoser]
public class VariationLookupBenchmarks
{
    private ulong _variationKey;

    [GlobalSetup]
    public void Setup() => _variationKey = GeneratedData.VariationKeys[GeneratedData.VariationKeys.Length / 2];

    [Benchmark]
    public int RegisteredVariationSequenceLookup()
    {
        int index = Array.BinarySearch(GeneratedData.VariationKeys, _variationKey);
        return index >= 0 ? GeneratedData.VariationEntryIndices[index] : -1;
    }
}

[MemoryDiagnoser]
public class SupportBenchmarks
{
    [Params("JisX0208", "JisX0213Plane1", "JisX0213Plane2", "JisX0213",
        "OutsideCharacterSet", "TsujiIvs_JisX0208", "TsujiIvs_Plane1", "TsujiIvs_Plane2",
        "TsujiIvs_JisX0213", "RegisteredSvs")]
    public string Scenario { get; set; } = "JisX0208";

    private KanjiCharacter _character;
    private CharacterSet _characterSet;

    [GlobalSetup]
    public void Setup()
    {
        (string text, _characterSet) = Scenario switch
        {
            "JisX0208" => ("高", CharacterSet.JisX0208),
            "JisX0213Plane1" => ("丑", CharacterSet.JisX0213Plane1),
            "JisX0213Plane2" => ("\u3406", CharacterSet.JisX0213Plane2),
            "JisX0213" => ("丑", CharacterSet.JisX0213),
            "OutsideCharacterSet" => ("\u3404", CharacterSet.JisX0213Plane1),
            "TsujiIvs_JisX0208" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208),
            "TsujiIvs_Plane1" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0213Plane1),
            "TsujiIvs_Plane2" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0213Plane2),
            "TsujiIvs_JisX0213" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0213),
            "RegisteredSvs" => ("不\uFE00", CharacterSet.JisX0208),
            _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
        };
        _character = KanjiCharacter.Parse(text);
    }

    [Benchmark]
    public bool IsSupported() => Kanji.IsSupported(_character, _characterSet);
}

[MemoryDiagnoser]
public class TextSupportBenchmarks
{
    [Params("JisX0208Supported", "JisX0208Unsupported", "JisX0213Plane1", "JisX0213Plane2", "RegisteredIvs")]
    public string Scenario { get; set; } = "JisX0208Supported";

    private string _text = string.Empty;
    private CharacterSet _characterSet;

    [GlobalSetup]
    public void Setup() => (_text, _characterSet) = Scenario switch
    {
        "JisX0208Supported" => ("高橋吉野", CharacterSet.JisX0208),
        "JisX0208Unsupported" => ("髙橋𠮷野", CharacterSet.JisX0208),
        "JisX0213Plane1" => ("ABC丑", CharacterSet.JisX0213Plane1),
        "JisX0213Plane2" => ("ABC\u3406", CharacterSet.JisX0213Plane2),
        "RegisteredIvs" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208),
        _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
    };

    [Benchmark]
    public bool IsSupported() => KanjiText.IsSupported(_text, _characterSet);
}

[MemoryDiagnoser]
public class TryGetAlternativeBenchmarks
{
    [Params("JisX0208_Unique", "Plane1_Unique", "Plane2_Unique", "JisX0213_Unique",
        "NoUniqueMapping", "RegisteredIvs_NoFallback", "RegisteredIvs_WithFallback", "RegisteredSvs_WithFallback")]
    public string Scenario { get; set; } = "JisX0208_Unique";

    private KanjiCharacter _character;
    private CharacterSet _characterSet;
    private KanjiFallbackOptions _options;

    [GlobalSetup]
    public void Setup()
    {
        (string text, _characterSet, _options) = Scenario switch
        {
            "JisX0208_Unique" => ("髙", CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "Plane1_Unique" => ("髙", CharacterSet.JisX0213Plane1, KanjiFallbackOptions.None),
            "Plane2_Unique" => ("\u343A", CharacterSet.JisX0213Plane2, KanjiFallbackOptions.None),
            "JisX0213_Unique" => ("\u343A", CharacterSet.JisX0213, KanjiFallbackOptions.None),
            "NoUniqueMapping" => ("〻", CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "RegisteredIvs_NoFallback" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "RegisteredIvs_WithFallback" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            "RegisteredSvs_WithFallback" => ("不\uFE00", CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
        };
        _character = KanjiCharacter.Parse(text);
    }

    [Benchmark]
    public bool TryGetAlternative() => Kanji.TryGetAlternative(_character, _characterSet, out _, _options);
}

[MemoryDiagnoser]
public class GetAlternativesBenchmarks
{
    [Params("SingleCandidate", "MultipleCandidates", "NoCandidates", "JisX0213Plane1",
        "JisX0213Plane2", "JisX0213", "RegisteredIvs_WithFallback", "RegisteredSvs_WithFallback")]
    public string Scenario { get; set; } = "SingleCandidate";

    private KanjiCharacter _character;
    private CharacterSet _characterSet;
    private KanjiFallbackOptions _options;

    [GlobalSetup]
    public void Setup()
    {
        (string text, _characterSet, _options) = Scenario switch
        {
            "SingleCandidate" => ("髙", CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "MultipleCandidates" => ("\u3404", CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "NoCandidates" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "JisX0213Plane1" => ("\u343A", CharacterSet.JisX0213Plane1, KanjiFallbackOptions.None),
            "JisX0213Plane2" => ("\u343A", CharacterSet.JisX0213Plane2, KanjiFallbackOptions.None),
            "JisX0213" => ("\u343A", CharacterSet.JisX0213, KanjiFallbackOptions.None),
            "RegisteredIvs_WithFallback" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            "RegisteredSvs_WithFallback" => ("不\uFE00", CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
        };
        _character = KanjiCharacter.Parse(text);
    }

    [Benchmark]
    public IReadOnlyList<KanjiCharacter> GetAlternatives() =>
        Kanji.GetAlternatives(_character, _characterSet, _options);
}

[MemoryDiagnoser]
public class ReplaceBenchmarks
{
    [Params("ASCII only", "No replacement", "Mixed ASCII and kanji", "One replacement",
        "Many replacements", "Supplementary", "IVS", "SVS",
        "JisX0213Plane1_NoReplacement", "JisX0213Plane1_WithReplacement",
        "JisX0213Plane2_NoReplacement", "JisX0213Plane2_WithReplacement",
        "JisX0213_MixedPlanes", "JisX0213_WithReplacement", "JisX0213_NoReplacement")]
    public string Scenario { get; set; } = "ASCII only";

    private string _text = string.Empty;
    private CharacterSet _characterSet;

    // シナリオと文字列は測定前に固定し、Replaceの処理だけを計測します。
    [GlobalSetup]
    public void Setup() => (_text, _characterSet) = Scenario switch
    {
        "ASCII only" => ("ABC123456789", CharacterSet.JisX0208),
        "No replacement" => ("高橋吉野", CharacterSet.JisX0208),
        "Mixed ASCII and kanji" => ("ABC高橋-123", CharacterSet.JisX0208),
        "One replacement" => ("髙橋吉野", CharacterSet.JisX0208),
        "Many replacements" => ("髙橋𠮷野髙橋𠮷野", CharacterSet.JisX0208),
        "Supplementary" => ("𠮷野", CharacterSet.JisX0208),
        "IVS" => ("\u3404\U000E0101", CharacterSet.JisX0208),
        "SVS" => ("\u6B04\uFE00", CharacterSet.JisX0208),
        "JisX0213Plane1_NoReplacement" => ("丑高", CharacterSet.JisX0213Plane1),
        "JisX0213Plane1_WithReplacement" => ("髙丑", CharacterSet.JisX0213Plane1),
        "JisX0213Plane2_NoReplacement" => ("\u3406", CharacterSet.JisX0213Plane2),
        "JisX0213Plane2_WithReplacement" => ("\u343A", CharacterSet.JisX0213Plane2),
        "JisX0213_MixedPlanes" => ("丑\u3406", CharacterSet.JisX0213),
        "JisX0213_WithReplacement" => ("髙\u3406", CharacterSet.JisX0213),
        "JisX0213_NoReplacement" => ("高丑\u3406", CharacterSet.JisX0213),
        _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
    };

    [Benchmark]
    public string Replace() => KanjiText.Replace(_text, _characterSet);
}

[MemoryDiagnoser]
public class VariationSequenceBenchmarks
{
    [Params("RegisteredIvs_NoFallback", "RegisteredIvs_WithFallback", "RegisteredSvs_WithFallback",
        "MjUniqueMapping_Priority", "FallbackDoesNotChain", "UnregisteredVariationSequence")]
    public string Scenario { get; set; } = "RegisteredIvs_NoFallback";

    private string _text = string.Empty;
    private CharacterSet _characterSet;
    private KanjiFallbackOptions _options;

    [GlobalSetup]
    public void Setup()
    {
        (_text, _characterSet, _options) = Scenario switch
        {
            "RegisteredIvs_NoFallback" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208, KanjiFallbackOptions.None),
            "RegisteredIvs_WithFallback" => (BenchmarkFixtures.TsujiIvs, CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            "RegisteredSvs_WithFallback" => ("不\uFE00", CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            "MjUniqueMapping_Priority" => ("亟\U000E0102", CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            "FallbackDoesNotChain" => ("\u59EC\uFE00", CharacterSet.JisX0213Plane1, KanjiFallbackOptions.AllowVariationSelectorFallback),
            "UnregisteredVariationSequence" => ("髙\uFE0F", CharacterSet.JisX0208, KanjiFallbackOptions.AllowVariationSelectorFallback),
            _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
        };
    }

    [Benchmark]
    public string ReplaceVariationSequence() => KanjiText.Replace(_text, _characterSet, _options);
}

[MemoryDiagnoser]
public class LongTextBenchmarks
{
    [Params("ASCII only", "Kanji no replacement", "Kanji high replacement",
        "ASCII and kanji mixed", "JisX0208 and JisX0213 mixed", "Several IVS and SVS",
        "Low replacement rate ~10%", "Replacement rate ~50%")]
    public string Scenario { get; set; } = "ASCII only";

    private string _text = string.Empty;
    private CharacterSet _characterSet;

    // 長さや置換率の異なる固定入力を準備時に組み立て、測定中のfixture生成を避けます。
    [GlobalSetup]
    public void Setup() => (_text, _characterSet) = Scenario switch
    {
        "ASCII only" => (new string('A', 100), CharacterSet.JisX0208),
        "Kanji no replacement" => (string.Concat(Enumerable.Repeat("高橋吉野", 25)), CharacterSet.JisX0208),
        "Kanji high replacement" => (string.Concat(Enumerable.Repeat("髙𠮷", 50)), CharacterSet.JisX0208),
        "ASCII and kanji mixed" => (string.Concat(Enumerable.Repeat("ABCD高橋1234野", 9)), CharacterSet.JisX0208),
        "JisX0208 and JisX0213 mixed" => (string.Concat(Enumerable.Repeat("丑\u3406", 50)), CharacterSet.JisX0213),
        "Several IVS and SVS" => (string.Concat(new string('高', 85), BenchmarkFixtures.TsujiIvs,
            BenchmarkFixtures.SakakiIvs, "不\uFE00", "\u3406\U000E0100", "亟\U000E0102"), CharacterSet.JisX0208),
        "Low replacement rate ~10%" => (new string('高', 90) + new string('髙', 10), CharacterSet.JisX0208),
        "Replacement rate ~50%" => (string.Concat(Enumerable.Repeat("高髙", 50)), CharacterSet.JisX0208),
        _ => throw new ArgumentOutOfRangeException(nameof(Scenario))
    };

    [Benchmark]
    public string ReplaceLongText() => KanjiText.Replace(_text, _characterSet);
}

internal static class BenchmarkFixtures
{
    // 各文字列は既存xUnitテストで登録状態と変換挙動を確認したfixtureです。
    internal const string TsujiIvs = "辻\U000E0100";
    internal const string SakakiIvs = "榊\U000E0100";
}
