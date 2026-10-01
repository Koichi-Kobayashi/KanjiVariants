// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using System.Text.RegularExpressions;
using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JisX0212Tests
{
    private const CharacterSet Set = CharacterSet.JisX0212;

    [Fact]
    public void CharacterSet_ExistingNumericValues_ArePreserved()
    {
        Assert.Equal(0, (int)CharacterSet.JisX0208);
        Assert.Equal(1, (int)CharacterSet.JisX0213Plane1);
        Assert.Equal(2, (int)CharacterSet.JisX0213Plane2);
        Assert.Equal(3, (int)CharacterSet.JisX0213);
        Assert.Equal(4, (int)Set);
    }

    // 固定ICU対応表と既存生成表で確認した所属差です。
    [Theory]
    [InlineData("丂", true, false, true)]
    [InlineData("丄", true, false, false)]
    [InlineData("高", false, true, true)]
    [InlineData("丑", false, true, true)]
    [InlineData("\U00020000", false, false, false)]
    public void IsSupported_IndependentCharacterSets_ReturnExpected(
        string text, bool jis0212, bool jis0208, bool jis0213)
    {
        Assert.Equal(jis0212, Kanji.IsSupported(text, Set));
        Assert.Equal(jis0212, Kanji.IsSupported(KanjiCharacter.Parse(text), Set));
        Assert.Equal(jis0208, Kanji.IsSupported(text, CharacterSet.JisX0208));
        Assert.Equal(jis0213, Kanji.IsSupported(text, CharacterSet.JisX0213));
    }

    [Theory]
    [InlineData("\u4E02\U000E0100")]
    [InlineData("\u4E41\uFE00")]
    public void IsSupported_RegisteredIvsOrSvs_DoesNotFallback(string text)
    {
        var character = KanjiCharacter.Parse(text);
        Assert.True(Kanji.IsSupported(character.BaseCharacter.ToString(), Set));
        Assert.False(Kanji.IsSupported(character, Set));
        Assert.False(Kanji.IsSupported(text, Set));
        Assert.False(KanjiText.IsSupported(text, Set));
    }

    [Theory]
    [InlineData("\u4E02\U000E0100")]
    [InlineData("\u4E41\uFE00")]
    public void Alternatives_RegisteredIvsOrSvs_WithFallback_ReturnSupportedReplacement(string text)
    {
        const KanjiFallbackOptions options = KanjiFallbackOptions.AllowVariationSelectorFallback;
        var character = KanjiCharacter.Parse(text);
        var candidates = Kanji.GetAlternatives(character, Set, options);
        Assert.Contains(KanjiCharacter.Parse(character.BaseCharacter.ToString()), candidates);
        Assert.All(candidates, candidate => Assert.True(Kanji.IsSupported(candidate, Set)));
        Assert.True(Kanji.TryGetAlternative(character, Set, out var replacement, options));
        Assert.True(Kanji.IsSupported(replacement, Set));
        Assert.Equal(replacement.ToString(), KanjiText.Replace(text, Set, options));
    }

    [Theory]
    [InlineData("")]
    [InlineData("高高")]
    [InlineData("\uD800")]
    [InlineData("\uDC00")]
    [InlineData("\u4E02\U000E01EF")]
    public void IsSupported_InvalidKanjiInput_PreservesFormatException(string text) =>
        Assert.Throws<FormatException>(() => Kanji.IsSupported(text, Set));

    [Fact]
    public void IsSupported_NullInput_PreservesArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Kanji.IsSupported((string)null!, Set));
        Assert.Throws<ArgumentNullException>(() => KanjiText.IsSupported(null!, Set));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("ASCII", true)]
    [InlineData("丂丄", true)]
    [InlineData("丂高", false)]
    [InlineData("\u00C4\u02D8", true)]
    [InlineData("\uD800", false)]
    [InlineData("\u4E02\U000E01EF", false)]
    public void KanjiTextIsSupported_WholeText_ReturnsExpected(string text, bool expected) =>
        Assert.Equal(expected, KanjiText.IsSupported(text, Set));

    [Fact]
    public void IsSupported_NonKanji_PreservesKanjiCharacterRestriction()
    {
        Assert.True(KanjiText.IsSupported("\u00C4", Set));
        Assert.Throws<FormatException>(() => Kanji.IsSupported("\u00C4", Set));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(5)]
    [InlineData(99)]
    public void IsSupported_UndefinedCharacterSet_PreservesArgumentOutOfRangeException(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Kanji.IsSupported("丂", (CharacterSet)value));
        Assert.Throws<ArgumentOutOfRangeException>(() => KanjiText.IsSupported("", (CharacterSet)value));
    }

    [Fact]
    public void Replace_SupportedText_ReturnsSameInstance()
    {
        string text = new(new[] { '丂', '丄', 'Ä' });
        Assert.Same(text, KanjiText.Replace(text, Set));
    }

    [Fact]
    public void IsSupported_AllFixedSourceCharacters_AndIntersectionsMatch()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "data", "JIS", "jisx-212.ucm")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        string path = Path.Combine(directory!.FullName, "data", "JIS", "jisx-212.ucm");
        var codePoints = new HashSet<int>();
        int han = 0, supplementary = 0, overlap0208 = 0, overlap0213 = 0;
        foreach (string line in File.ReadLines(path))
        {
            var match = Regex.Match(line, @"^<U([0-9A-F]{4,6})> ");
            if (!match.Success) continue;
            int cp = Convert.ToInt32(match.Groups[1].Value, 16);
            Assert.True(Rune.IsValid(cp));
            Assert.True(codePoints.Add(cp));
            string text = new Rune(cp).ToString();
            Assert.True(KanjiText.IsSupported(text, Set), $"U+{cp:X}");
            if (cp > 0xFFFF) supplementary++;
            if (cp >= 0x4E00 && cp <= 0x9FFF)
            {
                han++;
                Assert.True(Kanji.IsSupported(text, Set), $"U+{cp:X}");
            }
            if (KanjiText.IsSupported(text, CharacterSet.JisX0208) && cp >= 0x80) overlap0208++;
            if (KanjiText.IsSupported(text, CharacterSet.JisX0213) && cp >= 0x80) overlap0213++;
        }
        Assert.Equal(6067, codePoints.Count);
        Assert.Equal(5801, han);
        Assert.Equal(0, supplementary);
        Assert.Equal(0, overlap0208);
        Assert.Equal(2751, overlap0213);
    }
}
