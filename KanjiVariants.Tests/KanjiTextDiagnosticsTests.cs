// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class KanjiTextDiagnosticsTests
{
    private const CharacterSet Set = CharacterSet.JisX0208;
    private const KanjiFallbackOptions AllowVs = KanjiFallbackOptions.AllowVariationSelectorFallback;

    [Theory]
    [InlineData("A", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.Supported, null)]
    [InlineData("漢", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.Supported, null)]
    [InlineData("\u3404", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.NoReplacement, null)]
    [InlineData("あ", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.Supported, null)]
    [InlineData("😀", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.NoReplacement, null)]
    [InlineData("𠮷", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.MjUniqueMappingAvailable, "吉")]
    [InlineData("髙", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.MjUniqueMappingAvailable, "高")]
    [InlineData("㐺", CharacterSet.JisX0213Plane1, KanjiFallbackOptions.None, KanjiDiagnosticReason.UniqueAlternativeNotInTargetCharacterSet, null)]
    [InlineData("辻\U000E0100", Set, KanjiFallbackOptions.None, KanjiDiagnosticReason.NoReplacement, null)]
    [InlineData("辻\U000E0100", Set, AllowVs, KanjiDiagnosticReason.VariationSelectorFallbackAvailable, "辻")]
    [InlineData("亟\U000E0102", Set, AllowVs, KanjiDiagnosticReason.MjUniqueMappingAvailable, "丞")]
    [InlineData("辻\U000E0102", Set, AllowVs, KanjiDiagnosticReason.MjUniqueMappingAvailable, "辻")]
    [InlineData("姬\uFE00", CharacterSet.JisX0213Plane1, AllowVs, KanjiDiagnosticReason.VariationSelectorFallbackAvailable, "姬")]
    [InlineData("不\uFE00", Set, AllowVs, KanjiDiagnosticReason.VariationSelectorFallbackAvailable, "不")]
    [InlineData("髙\uFE0F", Set, AllowVs, KanjiDiagnosticReason.UnregisteredVariationSequence, null)]
    [InlineData("髙\U000E01EF", Set, AllowVs, KanjiDiagnosticReason.UnregisteredVariationSequence, null)]
    [InlineData("A\uFE00", Set, AllowVs, KanjiDiagnosticReason.UnregisteredVariationSequence, null)]
    [InlineData("\uFE00", Set, AllowVs, KanjiDiagnosticReason.NoReplacement, null)]
    [InlineData("\U000E0100", Set, AllowVs, KanjiDiagnosticReason.NoReplacement, null)]
    [InlineData("\uFE00\U000E0100", Set, AllowVs, KanjiDiagnosticReason.NoReplacement, null)]
    public void AnalyzeText_OneUnit_ReturnsExpectedReasonAndActualReplacement(string text,
        CharacterSet set, KanjiFallbackOptions options, KanjiDiagnosticReason reason, string? replacement)
    {
        var result = KanjiDiagnostics.AnalyzeText(text, set, options);
        var item = Assert.Single(result.Items);
        Assert.Equal(reason, item.Reason);
        Assert.Equal(replacement, item.Replacement);
        AssertConsistent(text, set, options);
    }

    [Fact]
    public void AnalyzeText_Empty_PreservesInputAndSettings()
    {
        var result = KanjiDiagnostics.AnalyzeText("", CharacterSet.JisX0212, AllowVs);
        Assert.Same("", result.Text);
        Assert.Equal(CharacterSet.JisX0212, result.CharacterSet);
        Assert.Equal(AllowVs, result.Options);
        Assert.True(result.IsSupported);
        Assert.Empty(result.Items);
    }

    [Fact]
    public void AnalyzeText_Positions_PreserveSupplementaryIvsMalformedUtf16AndCrLf()
    {
        string broken = new('\uDB40', 1);
        string input = "A𠮷辻\U000E0100" + broken + "\r\n高";
        var result = KanjiDiagnostics.AnalyzeText(input, Set, AllowVs);
        Assert.Equal(new[] { 0, 1, 3, 6, 7, 8, 9 }, result.Items.Select(x => x.Utf16Index));
        Assert.Equal(new[] { 1, 2, 3, 1, 1, 1, 1 }, result.Items.Select(x => x.Utf16Length));
        Assert.Equal(KanjiDiagnosticReason.InvalidUtf16, result.Items[3].Reason);
        Assert.Equal(broken, result.Items[3].Original);
        AssertConsistent(input, Set, AllowVs);
        var supplementaryIvs = Assert.Single(KanjiDiagnostics.AnalyzeText("\U00020509\U000E0103", Set).Items);
        Assert.Equal(4, supplementaryIvs.Utf16Length);
    }

    [Theory]
    [InlineData('\uD800')]
    [InlineData('\uDC00')]
    [InlineData('\uDB40')]
    public void AnalyzeText_IsolatedSurrogate_ReturnsInvalidItemAndContinues(char surrogate)
    {
        string broken = new(surrogate, 1);
        foreach (string text in new[] { broken, "髙" + broken, broken + "A𠮷", "髙" + broken + "A𠮷" })
        {
            var result = KanjiDiagnostics.AnalyzeText(text, Set, AllowVs);
            var invalid = Assert.Single(result.Items, x => x.Reason == KanjiDiagnosticReason.InvalidUtf16);
            Assert.Equal(broken, invalid.Original);
            Assert.Equal(1, invalid.Utf16Length);
            AssertConsistent(text, Set, AllowVs);
        }
    }

    [Fact]
    public void AnalyzeText_ConsecutiveSelectors_PreservesReaderBoundaries()
    {
        var result = KanjiDiagnostics.AnalyzeText("辻\U000E0100\uFE00𠮷", Set, AllowVs);
        Assert.Equal(new[] { "辻\U000E0100", "\uFE00", "𠮷" }, result.Items.Select(x => x.Original));
        Assert.Equal(KanjiDiagnosticReason.NoReplacement, result.Items[1].Reason);
        AssertConsistent(result.Text, Set, AllowVs);
    }

    [Fact]
    public void AnalyzeText_Items_AreReadOnlyAndInputIsShared()
    {
        string input = new("髙A".ToCharArray());
        var result = KanjiDiagnostics.AnalyzeText(input, Set);
        Assert.Same(input, result.Text);
        Assert.Throws<NotSupportedException>(() => ((IList<KanjiTextDiagnosticItem>)result.Items).Clear());
    }

    [Fact]
    public void AnalyzeText_InvalidArguments_PreserveValidationOrderAndParamNames()
    {
        var invalidSet = (CharacterSet)99;
        var invalidOptions = (KanjiFallbackOptions)0x4001;
        Assert.Throws<ArgumentNullException>("text", () => KanjiDiagnostics.AnalyzeText(null!, invalidSet, invalidOptions));
        Assert.Throws<ArgumentOutOfRangeException>("characterSet", () => KanjiDiagnostics.AnalyzeText("", invalidSet, invalidOptions));
        Assert.Throws<ArgumentOutOfRangeException>("options", () => KanjiDiagnostics.AnalyzeText("", Set, invalidOptions));
    }

    [Theory]
    [InlineData(CharacterSet.JisX0208)]
    [InlineData(CharacterSet.JisX0213Plane1)]
    [InlineData(CharacterSet.JisX0213Plane2)]
    [InlineData(CharacterSet.JisX0213)]
    [InlineData(CharacterSet.JisX0212)]
    public void AnalyzeText_MixedCorpus_MatchesBothTextApisForEachSetAndOption(CharacterSet set)
    {
        string brokenHigh = new('\uD800', 1), brokenLow = new('\uDC00', 1);
        string[] texts = { "", "ABC123\0\r\n", "漢字漢字", "山田太郎", "髙橋𠮷野", "あ①😀",
            "丂㐺", "辻\U000E0100榊\U000E0100", "不\uFE00髙\uFE0F", "A\U000E01EF",
            "\uFE00\U000E0100髙", "髙\uFDFF𠮷", "髙\uFE10𠮷",
            "髙\uDB40\uDCFF𠮷", "髙\uDB40\uDDF0𠮷",
            "髙" + brokenHigh + "A𠮷" + brokenLow, brokenHigh + brokenHigh + brokenLow,
            "漢" + new string('髙', 64), new string('漢', 256), new string('髙', 1024) };
        foreach (var options in new[] { KanjiFallbackOptions.None, AllowVs })
            foreach (string text in texts)
                AssertConsistent(text, set, options);
    }

    [Fact]
    public void AnalyzeText_AllMjVariationKeys_MatchBothTextApis()
    {
        // 代表例だけでなく、GeneratedData.VariationKeys の全シーケンスを各集合・両オプションで照合します。
        foreach (ulong key in GeneratedData.VariationKeys)
        {
            string text = new Rune((int)(key & 0x1FFFFF)).ToString() + new Rune((int)(key >> 21)).ToString();
            foreach (var set in Enum.GetValues<CharacterSet>())
                foreach (var options in new[] { KanjiFallbackOptions.None, AllowVs })
                    AssertConsistent(text, set, options);
        }
    }

    private static void AssertConsistent(string text, CharacterSet set, KanjiFallbackOptions options)
    {
        var result = KanjiDiagnostics.AnalyzeText(text, set, options);
        Assert.Same(text, result.Text);
        Assert.Equal(set, result.CharacterSet);
        Assert.Equal(options, result.Options);
        Assert.Equal(KanjiText.IsSupported(text, set), result.IsSupported);
        var rebuilt = new StringBuilder();
        int offset = 0;
        bool supported = true;
        foreach (var item in result.Items)
        {
            Assert.Equal(offset, item.Utf16Index);
            Assert.Equal(item.Original.Length, item.Utf16Length);
            Assert.InRange(item.Utf16Length, 1, 4);
            Assert.Equal(text.Substring(offset, item.Utf16Length), item.Original);
            Assert.Equal(item.Reason == KanjiDiagnosticReason.Supported, item.IsSupported);
            Assert.Equal(item.Reason is KanjiDiagnosticReason.MjUniqueMappingAvailable or
                KanjiDiagnosticReason.VariationSelectorFallbackAvailable, item.Replacement is not null);
            if (item.Replacement is not null)
            {
                Assert.False(item.IsSupported);
                Assert.True(KanjiText.IsSupported(item.Replacement, set));
            }
            offset += item.Utf16Length;
            supported &= item.IsSupported;
            rebuilt.Append(item.Replacement ?? item.Original);
        }
        Assert.Equal(text.Length, offset);
        Assert.Equal(supported, result.IsSupported);
        Assert.Equal(KanjiText.Replace(text, set, options), rebuilt.ToString());
    }
}
