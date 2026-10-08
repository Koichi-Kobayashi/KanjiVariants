// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class TextKanjiClassificationTests
{
    // Unicode 18.0.0の拡張J割当範囲はU+323B0～U+33479です。
    // 既存範囲の末尾、拡張Jの先頭・中間・末尾、未割当部分を区別します。
    [Theory]
    [InlineData(0x323AF, true)]
    [InlineData(0x323B0, true)]
    [InlineData(0x32C00, true)]
    [InlineData(0x33478, true)]
    [InlineData(0x33479, true)]
    [InlineData(0x3347A, false)]
    [InlineData(0x3347F, false)]
    [InlineData(0x33480, false)]
    [InlineData(0x3400, true)]
    [InlineData(0x4DBF, true)]
    [InlineData(0x9FFF, true)]
    [InlineData(0x20000, true)]
    [InlineData(0x2A6DF, true)]
    [InlineData(0x2B740, true)]
    [InlineData(0x2B81D, true)]
    [InlineData(0x2B81E, true)]
    [InlineData(0x2B81F, false)]
    [InlineData(0x2CEB0, true)]
    [InlineData(0x2EBF0, true)]
    [InlineData(0x2F800, true)]
    [InlineData(0x30000, true)]
    [InlineData(0x31350, true)]
    public void AnalyzeText_CjkBoundaries_ClassifyWithoutChangingUtf16Units(int codePoint, bool isKanji)
    {
        string original = new Rune(codePoint).ToString();
        string text = "あ" + original + "A";
        var joyo = JoyoKanjiDiagnostics.AnalyzeText(text);
        var education = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade6);
        Assert.Equal(3, joyo.Items.Count);
        Assert.Equal(3, education.Items.Count);

        var joyoItem = joyo.Items[1];
        Assert.Equal(original, joyoItem.Original);
        Assert.Equal(1, joyoItem.Utf16Index);
        Assert.Equal(original.Length, joyoItem.Utf16Length);
        Assert.Equal(isKanji ? JoyoKanjiDiagnosticReason.NotJoyoKanji : JoyoKanjiDiagnosticReason.NotKanji, joyoItem.Reason);
        Assert.False(joyoItem.IsJoyoKanji);
        Assert.Equal(!isKanji, joyo.ContainsOnlyJoyoKanji);
        Assert.Equal(isKanji, joyo.HasNonJoyoKanji);
        Assert.False(joyo.HasInvalidUtf16);
        Assert.Equal(isKanji ? 1 : 0, joyo.NonJoyoItems.Count);
        if (isKanji) Assert.Same(joyoItem, joyo.NonJoyoItems[0]);

        var educationItem = education.Items[1];
        Assert.Equal(original, educationItem.Original);
        Assert.Equal(1, educationItem.Utf16Index);
        Assert.Equal(original.Length, educationItem.Utf16Length);
        Assert.Equal(isKanji ? EducationKanjiDiagnosticReason.NotEducationKanji : EducationKanjiDiagnosticReason.NotKanji, educationItem.Reason);
        Assert.Null(educationItem.Grade);
        Assert.Equal(!isKanji, educationItem.IsLearned);
        Assert.Equal(!isKanji, education.ContainsOnlyLearnedKanji);
        Assert.Equal(isKanji, education.HasNonEducationKanji);
        Assert.Null(education.HighestEducationKanjiGrade);
        Assert.Equal(isKanji ? 1 : 0, education.UnlearnedItems.Count);
        if (isKanji) Assert.Same(educationItem, education.UnlearnedItems[0]);
        Assert.Equal(1 + original.Length, joyo.Items[2].Utf16Index);
        Assert.Equal(1 + original.Length, education.Items[2].Utf16Index);
    }

    // Lookupは連続範囲を受理しますが、文章診断はUnicode 18.0.0の
    // 拡張D割当末尾U+2B81Eと未割当U+2B81Fを区別します。
    [Theory]
    [InlineData(0x2B81D)]
    [InlineData(0x2B81E)]
    [InlineData(0x2B81F)]
    public void ExistingParseRange_ExtensionDBoundary_RemainsRecognized(int codePoint)
    {
        Assert.True(Lookup.IsRecognizedKanji(codePoint));
        string text = new Rune(codePoint).ToString();
        Assert.True(KanjiCharacter.TryParse(text, out var parsed));
        Assert.Equal(codePoint, parsed.BaseCharacter.Value);
        Assert.Equal(parsed, KanjiCharacter.Parse(text));
        foreach (CharacterSet set in Enum.GetValues<CharacterSet>())
        {
            Assert.False(Kanji.IsSupported(parsed, set));
            Assert.False(Kanji.IsSupported(text, set));
            Assert.Empty(Kanji.GetAlternatives(text, set));
            Assert.False(Kanji.TryGetAlternative(text, set, out _));
            Assert.Same(text, KanjiText.Replace(text, set));
            Assert.False(KanjiText.IsSupported(text, set));
        }
    }

    [Fact]
    public void AnalyzeText_ExtensionJOnly_IsOneNonJoyoAndNonEducationItem()
    {
        const string text = "\U000323B0";
        var joyo = JoyoKanjiDiagnostics.AnalyzeText(text);
        var education = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade6);
        Assert.Single(joyo.Items);
        Assert.Single(education.Items);
        Assert.Equal(0, joyo.Items[0].Utf16Index);
        Assert.Equal(0, education.Items[0].Utf16Index);
        Assert.Equal(2, joyo.Items[0].Utf16Length);
        Assert.Equal(2, education.Items[0].Utf16Length);
        Assert.Single(joyo.NonJoyoItems);
        Assert.Single(education.UnlearnedItems);
        Assert.False(joyo.ContainsOnlyJoyoKanji);
        Assert.False(education.ContainsOnlyLearnedKanji);
    }

    [Theory]
    [InlineData("\U000323B0\U000E0100")]
    [InlineData("\U00033479\uFE00")]
    public void AnalyzeText_ExtensionJWithUnregisteredVs_DoesNotFallback(string text)
    {
        Assert.False(KanjiCharacter.TryParse(text, out _));
        var joyo = Assert.Single(JoyoKanjiDiagnostics.AnalyzeText(text).Items);
        var education = Assert.Single(EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade6).Items);
        Assert.Equal(text, joyo.Original);
        Assert.Equal(text, education.Original);
        Assert.Equal(text.Length, joyo.Utf16Length);
        Assert.Equal(text.Length, education.Utf16Length);
        Assert.Equal(JoyoKanjiDiagnosticReason.NotJoyoKanji, joyo.Reason);
        Assert.Equal(EducationKanjiDiagnosticReason.NotEducationKanji, education.Reason);
        Assert.Null(education.Grade);
    }

    // 診断専用分類の追加で、1.1.0の解析範囲や置換経路を広げないことを検証します。
    [Theory]
    [InlineData(0x323B0)]
    [InlineData(0x32C00)]
    [InlineData(0x33479)]
    [InlineData(0x3347A)]
    public void ExistingApis_ExtensionJ_RemainOutsideParseRangeAndUnchanged(int codePoint)
    {
        string text = new Rune(codePoint).ToString();
        Assert.False(KanjiCharacter.TryParse(text, out _));
        Assert.Throws<FormatException>(() => KanjiCharacter.Parse(text));
        Assert.Throws<FormatException>(() => KanjiDiagnostics.Analyze(text));
        foreach (CharacterSet set in Enum.GetValues<CharacterSet>())
        {
            Assert.Throws<FormatException>(() => Kanji.IsSupported(text, set));
            Assert.Throws<FormatException>(() => Kanji.GetAlternatives(text, set));
            Assert.Throws<FormatException>(() => Kanji.TryGetAlternative(text, set, out _));
            Assert.False(KanjiText.IsSupported(text, set));
            foreach (var options in new[] { KanjiFallbackOptions.None, KanjiFallbackOptions.AllowVariationSelectorFallback })
            {
                Assert.Same(text, KanjiText.Replace(text, set, options));
                var diagnostic = KanjiDiagnostics.AnalyzeText(text, set, options);
                Assert.False(diagnostic.IsSupported);
                var item = Assert.Single(diagnostic.Items);
                Assert.Equal(KanjiDiagnosticReason.NoReplacement, item.Reason);
                Assert.Null(item.Replacement);
            }
        }
    }
}
