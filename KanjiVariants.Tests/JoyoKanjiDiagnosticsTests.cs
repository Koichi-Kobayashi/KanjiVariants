// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JoyoKanjiDiagnosticsTests
{
    [Fact]
    public void AnalyzeText_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>("text", () => JoyoKanjiDiagnostics.AnalyzeText(null!));

    [Fact]
    public void AnalyzeText_Empty_IsValidWithSharedEmptyLists()
    {
        var result = JoyoKanjiDiagnostics.AnalyzeText("");
        Assert.Same("", result.Text);
        Assert.Empty(result.Items);
        Assert.Empty(result.NonJoyoItems);
        Assert.Same(result.Items, result.NonJoyoItems);
        Assert.True(result.ContainsOnlyJoyoKanji);
        Assert.False(result.HasNonJoyoKanji);
        Assert.False(result.HasInvalidUtf16);
    }

    [Theory]
    [InlineData("高", JoyoKanjiDiagnosticReason.JoyoKanji)]
    [InlineData("会", JoyoKanjiDiagnosticReason.JoyoKanji)]
    [InlineData("議", JoyoKanjiDiagnosticReason.JoyoKanji)]
    [InlineData("髙", JoyoKanjiDiagnosticReason.NotJoyoKanji)]
    [InlineData("會", JoyoKanjiDiagnosticReason.NotJoyoKanji)]
    [InlineData("\uF900", JoyoKanjiDiagnosticReason.NotJoyoKanji)]
    [InlineData("𠮷", JoyoKanjiDiagnosticReason.NotJoyoKanji)]
    [InlineData("\U0002F800", JoyoKanjiDiagnosticReason.NotJoyoKanji)]
    [InlineData("あ", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("ア", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("A", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("0", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("。", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("😀", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("々", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("〆", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("〻", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("\uFE00", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("\U000E0100", JoyoKanjiDiagnosticReason.NotKanji)]
    [InlineData("A\uFE00", JoyoKanjiDiagnosticReason.NotKanji)]
    public void AnalyzeText_SingleUnit_ReturnsExpectedReason(string text, JoyoKanjiDiagnosticReason reason)
    {
        var result = JoyoKanjiDiagnostics.AnalyzeText(text);
        var item = Assert.Single(result.Items);
        Assert.Equal(text, item.Original);
        Assert.Equal(reason, item.Reason);
        Assert.Equal(reason == JoyoKanjiDiagnosticReason.JoyoKanji, item.IsJoyoKanji);
        Assert.Equal(reason != JoyoKanjiDiagnosticReason.NotJoyoKanji, result.ContainsOnlyJoyoKanji);
        AssertConsistent(text);
    }

    [Theory]
    [InlineData("辻\U000E0100")]
    [InlineData("不\uFE00")]
    [InlineData("欄\uFE00")]
    public void AnalyzeText_RegisteredIvsOrSvs_IsNotJoyoAndIsNotConverted(string text)
    {
        Assert.True(KanjiCharacter.TryParse(text, out _));
        var item = Assert.Single(JoyoKanjiDiagnostics.AnalyzeText(text).Items);
        Assert.Equal(text, item.Original);
        Assert.Equal(JoyoKanjiDiagnosticReason.NotJoyoKanji, item.Reason);
        AssertConsistent(text);
    }

    [Fact]
    public void AnalyzeText_UnregisteredVariationSequence_OnJoyoBaseIsNotJoyo()
    {
        const string text = "学\U000E01EF";
        Assert.True(JoyoKanji.IsJoyo("学"));
        Assert.False(KanjiCharacter.TryParse(text, out _));
        var item = Assert.Single(JoyoKanjiDiagnostics.AnalyzeText(text).Items);
        Assert.Equal(3, item.Utf16Length);
        Assert.Equal(JoyoKanjiDiagnosticReason.NotJoyoKanji, item.Reason);
        AssertConsistent(text);
    }

    [Theory]
    [InlineData("明日は会議です。", false)]
    [InlineData("明日は髙橋さんと會議です。", true)]
    public void AnalyzeText_Sentence_PreservesNonJoyoOrderAndDoesNotConvert(string text, bool hasNonJoyo)
    {
        var result = JoyoKanjiDiagnostics.AnalyzeText(text);
        Assert.Equal(hasNonJoyo, result.HasNonJoyoKanji);
        Assert.Equal(!hasNonJoyo, result.ContainsOnlyJoyoKanji);
        Assert.Equal(hasNonJoyo ? new[] { "髙", "會" } : Array.Empty<string>(), result.NonJoyoItems.Select(x => x.Original));
        if (hasNonJoyo)
            Assert.Equal(new[] { 3, 8 }, result.NonJoyoItems.Select(x => x.Utf16Index));
        AssertConsistent(text);
    }

    [Theory]
    [InlineData('\uD800')]
    [InlineData('\uDC00')]
    public void AnalyzeText_LoneSurrogate_IsInvalidAndExcludedFromNonJoyo(char surrogate)
    {
        string text = new(surrogate, 1);
        var result = JoyoKanjiDiagnostics.AnalyzeText(text);
        Assert.Equal(JoyoKanjiDiagnosticReason.InvalidUtf16, Assert.Single(result.Items).Reason);
        Assert.False(result.ContainsOnlyJoyoKanji);
        Assert.False(result.HasNonJoyoKanji);
        Assert.True(result.HasInvalidUtf16);
        Assert.Empty(result.NonJoyoItems);
        AssertConsistent(text);
    }

    [Theory]
    [InlineData("\r\n")]
    [InlineData("\n")]
    public void AnalyzeText_LineEndings_PreservesSeparateUnits(string newline)
    {
        var result = JoyoKanjiDiagnostics.AnalyzeText("高" + newline + "會");
        Assert.Equal(newline.Length + 2, result.Items.Count);
        Assert.Equal(1 + newline.Length, Assert.Single(result.NonJoyoItems).Utf16Index);
        Assert.All(result.Items, item => Assert.Equal(1, item.Utf16Length));
        AssertConsistent(result.Text);
    }

    [Fact]
    public void AnalyzeText_MixedUnits_MatchesEducationReaderAndPreservesInvalidUtf16()
    {
        string text = "A𠮷高\U000E0100不\uFE00" + new string('\uD800', 1) + "會\r\n";
        var result = JoyoKanjiDiagnostics.AnalyzeText(text);
        Assert.Equal(new[] { 0, 1, 3, 6, 8, 9, 10, 11 }, result.Items.Select(x => x.Utf16Index));
        Assert.Equal(new[] { 1, 2, 3, 2, 1, 1, 1, 1 }, result.Items.Select(x => x.Utf16Length));
        Assert.Equal(new[] { "𠮷", "高\U000E0100", "不\uFE00", "會" }, result.NonJoyoItems.Select(x => x.Original));
        Assert.True(result.HasNonJoyoKanji);
        Assert.True(result.HasInvalidUtf16);
        Assert.False(result.ContainsOnlyJoyoKanji);
        AssertConsistent(text);
        AssertConsistent("𠮷\U000E0100\uFE00高" + new string(new[] { '\uD800', '\uD800', '\uDC00', '\uDC00' }));
    }

    [Fact]
    public void AnalyzeText_Collections_AreReadOnlyAndShareNonJoyoInstances()
    {
        var result = JoyoKanjiDiagnostics.AnalyzeText("髙高會髙");
        Assert.Equal(new[] { "髙", "會", "髙" }, result.NonJoyoItems.Select(x => x.Original));
        Assert.Same(result.Items[0], result.NonJoyoItems[0]);
        Assert.Same(result.Items[2], result.NonJoyoItems[1]);
        Assert.Same(result.Items[3], result.NonJoyoItems[2]);
        Assert.Throws<NotSupportedException>(() => ((IList<JoyoKanjiDiagnosticItem>)result.Items).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<JoyoKanjiDiagnosticItem>)result.NonJoyoItems).Clear());
    }

    [Fact]
    public void AnalyzeText_All2136JoyoKanji_AreJoyoAndContainNoViolations()
    {
        string text = string.Concat(GeneratedJoyoKanjiData.CodePoints.Select(cp => new System.Text.Rune(cp).ToString()));
        var result = JoyoKanjiDiagnostics.AnalyzeText(text);
        Assert.Equal(2136, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.IsJoyoKanji));
        Assert.Empty(result.NonJoyoItems);
        Assert.True(result.ContainsOnlyJoyoKanji);
        AssertConsistent(text);
    }

    private static void AssertConsistent(string text)
    {
        var result = JoyoKanjiDiagnostics.AnalyzeText(text);
        var education = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade6);
        Assert.Same(text, result.Text);
        Assert.Equal(text, string.Concat(result.Items.Select(x => x.Original)));
        Assert.Equal(education.Items.Select(x => (x.Original, x.Utf16Index, x.Utf16Length)),
            result.Items.Select(x => (x.Original, x.Utf16Index, x.Utf16Length)));
        Assert.Equal(result.Items.Where(x => x.Reason == JoyoKanjiDiagnosticReason.NotJoyoKanji), result.NonJoyoItems);
        Assert.Equal(result.NonJoyoItems.Count > 0, result.HasNonJoyoKanji);
        Assert.Equal(result.Items.Any(x => x.Reason == JoyoKanjiDiagnosticReason.InvalidUtf16), result.HasInvalidUtf16);
        Assert.Equal(result.Items.All(x => x.Reason is JoyoKanjiDiagnosticReason.NotKanji or JoyoKanjiDiagnosticReason.JoyoKanji),
            result.ContainsOnlyJoyoKanji);
        foreach (var item in result.Items)
        {
            Assert.Equal(text.Substring(item.Utf16Index, item.Utf16Length), item.Original);
            Assert.Equal(item.Original.Length, item.Utf16Length);
            Assert.Equal(JoyoKanji.IsJoyo(item.Original), item.IsJoyoKanji);
        }
    }
}
