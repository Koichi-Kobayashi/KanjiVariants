// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class VariationSelectorBoundaryTests
{
    private const CharacterSet Set = CharacterSet.JisX0208;
    private const KanjiFallbackOptions AllowVs = KanjiFallbackOptions.AllowVariationSelectorFallback;

    [Theory]
    [InlineData("髙\uFDFF", false, "高\uFDFF", "高\uFDFF")]
    [InlineData("髙\uFE00", true, "髙\uFE00", "髙\uFE00")]
    [InlineData("髙\uFE0F", true, "髙\uFE0F", "髙\uFE0F")]
    [InlineData("髙\uFE10", false, "高\uFE10", "高\uFE10")]
    [InlineData("髙\uDB40\uDCFF", false, "高\uDB40\uDCFF", "高\uDB40\uDCFF")]
    [InlineData("辻\uDB40\uDD00", true, "辻\uDB40\uDD00", "辻")]
    [InlineData("髙\uDB40\uDDEF", true, "髙\uDB40\uDDEF", "髙\uDB40\uDDEF")]
    [InlineData("髙\uDB40\uDDF0", false, "高\uDB40\uDDF0", "高\uDB40\uDDF0")]
    public void Replace_VsRangeBoundaries_ConsumesOnlyVsAndPreservesFollowingText(
        string input, bool isSelector, string expectedNone, string expectedFallback)
    {
        Assert.False(KanjiText.IsSupported(input, Set));
        foreach (var (options, expected) in new[]
        {
            (KanjiFallbackOptions.None, expectedNone), (AllowVs, expectedFallback)
        })
        {
            // 先頭付近と末尾を確認し、後続の置換対象を取り込まないことも確認します。
            Assert.Equal(expected, KanjiText.Replace(input, Set, options));
            Assert.Equal(expected + "吉", KanjiText.Replace(input + "𠮷", Set, options));
            // 先にbuilderが作られた経路でも、境界付近の符号単位を維持します。
            Assert.Equal("高" + expected + "吉", KanjiText.Replace("髙" + input + "𠮷", Set, options));
        }
        if (!isSelector)
        {
            // 範囲外なら髙だけが独立した置換単位になります。
            Assert.StartsWith("高", expectedFallback);
        }
        else if (input.StartsWith("髙", StringComparison.Ordinal))
        {
            // 髙とこのVSの組み合わせは固定Unicode原典に登録されていません。
            Assert.False(KanjiCharacter.TryParse(input, out _));
            Assert.Same(input, KanjiText.Replace(input, Set, AllowVs));
        }
    }

    [Theory]
    [InlineData('\uDB40')]
    [InlineData('\uD800')]
    [InlineData('\uDC00')]
    public void Replace_IsolatedSurrogate_PreservesUnitAndContinues(char surrogate)
    {
        // テストデータ転送で不正UTF-16が置換されないよう、実行時に組み立てます。
        string broken = new(surrogate, 1);
        Assert.False(KanjiText.IsSupported(broken, Set));
        Assert.Same(broken, KanjiText.Replace(broken, Set, AllowVs));
        Assert.Equal("高" + broken, KanjiText.Replace("髙" + broken, Set, AllowVs));
        Assert.Equal("高" + broken + "吉", KanjiText.Replace("髙" + broken + "𠮷", Set, AllowVs));
        Assert.Throws<FormatException>(() => KanjiCharacter.Parse(broken));
    }

    [Fact]
    public void Replace_HighSurrogateFollowedByOrdinaryCharacter_DoesNotConsumeIt()
    {
        string broken = new('\uDB40', 1);
        string input = "髙" + broken + "A𠮷";
        Assert.False(KanjiText.IsSupported(input, Set));
        Assert.Equal("高" + broken + "A吉", KanjiText.Replace(input, Set, AllowVs));
    }

    [Theory]
    [InlineData("\uFE00")]
    [InlineData("\U000E0100")]
    public void Replace_SelectorAlone_IsPreservedAndFollowingReplacementContinues(string selector)
    {
        Assert.False(KanjiText.IsSupported(selector, Set));
        Assert.Same(selector, KanjiText.Replace(selector, Set, AllowVs));
        Assert.Equal(selector + "吉", KanjiText.Replace(selector + "𠮷", Set, AllowVs));
    }

    [Theory]
    [InlineData("\uFE00")]
    [InlineData("\U000E0100")]
    public void Replace_ConsecutiveSelectors_FallbackConsumesOnlyFirstRegisteredSequence(string extra)
    {
        string input = "辻\U000E0100" + extra + "𠮷";
        Assert.False(KanjiText.IsSupported(input, Set));
        Assert.Equal("辻" + extra + "吉", KanjiText.Replace(input, Set, AllowVs));
        Assert.Equal("辻\U000E0100" + extra + "吉", KanjiText.Replace(input, Set));
    }
}
