// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class KanjiDiagnosticsTests
{
    // 補助平面本表字、IVS、SVSは既存テストの登録済み表現を使用します。
    [Theory]
    [InlineData("高", 0x9AD8, true, null, 1, 1)]
    [InlineData("\U00020B9F", 0x20B9F, false, null, 1, 2)]
    [InlineData("辻\U000E0102", 0x8FBB, true, 0xE0102, 2, 3)]
    [InlineData("\u6B04\uFE00", 0x6B04, true, 0xFE00, 2, 2)]
    public void Analyze_UnicodeRepresentation_ReturnsScalarAndUtf16Information(
        string text, int codePoint, bool bmp, int? selector, int scalars, int units)
    {
        var result = KanjiDiagnostics.Analyze(text);
        Assert.Equal(KanjiCharacter.Parse(text), result.Character);
        Assert.Equal(codePoint, result.BaseCodePoint);
        Assert.Equal(bmp, result.IsBaseCharacterBmp);
        Assert.Equal(selector.HasValue, result.HasVariationSelector);
        Assert.Equal(selector, result.VariationSelectorCodePoint);
        Assert.Equal(scalars, result.UnicodeScalarCount);
        Assert.Equal(units, result.Utf16CodeUnitCount);
        Assert.Equal(text.Length, result.Utf16CodeUnitCount);
    }

    [Theory]
    [InlineData("高")]
    [InlineData("髙")]
    [InlineData("一")]
    [InlineData("丑")]
    [InlineData("丂")]
    [InlineData("丄")]
    [InlineData("\U00020B9F")]
    [InlineData("辻\U000E0102")]
    [InlineData("\u6B04\uFE00")]
    [InlineData("亜\U000E0100")]
    public void Analyze_AllFields_MatchExistingApis(string text)
    {
        var character = KanjiCharacter.Parse(text);
        var result = KanjiDiagnostics.Analyze(character);
        Assert.Equal(result, KanjiDiagnostics.Analyze(text));
        Assert.Equal(Kanji.IsSupported(character, CharacterSet.JisX0208), result.IsJisX0208);
        Assert.Equal(Kanji.IsSupported(character, CharacterSet.JisX0212), result.IsJisX0212);
        Assert.Equal(Kanji.IsSupported(character, CharacterSet.JisX0213Plane1), result.IsJisX0213Plane1);
        Assert.Equal(Kanji.IsSupported(character, CharacterSet.JisX0213Plane2), result.IsJisX0213Plane2);
        Assert.Equal(Kanji.IsSupported(character, CharacterSet.JisX0213), result.IsJisX0213);
        Assert.Equal(JoyoKanji.IsJoyo(character), result.IsJoyo);
        Assert.Same(JoyoKanji.Get(character), result.JoyoEntry);
        Assert.Equal(EducationKanji.IsEducationKanji(character), result.IsEducationKanji);
        Assert.Equal(EducationKanji.GetGrade(character), result.EducationGrade);
        Assert.Equal(JinmeiyoKanji.IsJinmeiyoKanji(character), result.IsJinmeiyoKanji);
        Assert.Equal(JinmeiyoKanji.IsNameUsableKanji(character), result.IsNameUsableKanji);
        Assert.Same(JoyoKanjiEducation.GetReadings(character), result.ReadingEducation);
        Assert.Same(MjCharacter.Find(character), result.MjMatches);
    }

    [Fact]
    public void Analyze_JoyoEducationAndJinmeiyoFixtures_ReturnExpectedMembership()
    {
        var joyo = KanjiDiagnostics.Analyze("高");
        Assert.True(joyo.IsJoyo);
        Assert.NotNull(joyo.JoyoEntry);
        Assert.NotEmpty(joyo.ReadingEducation);
        Assert.True(joyo.IsNameUsableKanji);
        Assert.False(joyo.IsJinmeiyoKanji);
        var education = KanjiDiagnostics.Analyze("一");
        Assert.True(education.IsEducationKanji);
        Assert.Equal(KanjiGrade.Grade1, education.EducationGrade);
        var name = KanjiDiagnostics.Analyze("丑");
        Assert.True(name.IsJinmeiyoKanji);
        Assert.True(name.IsNameUsableKanji);
        Assert.False(name.IsJoyo);
        Assert.Null(name.JoyoEntry);
        Assert.Empty(name.ReadingEducation);
        var jis0212 = KanjiDiagnostics.Analyze("丂");
        Assert.True(jis0212.IsJisX0212);
        Assert.False(jis0212.IsJisX0208);
    }

    [Theory]
    [InlineData("亜\U000E0100")]
    [InlineData("辻\U000E0102")]
    [InlineData("\u6B04\uFE00")]
    public void Analyze_VariationSequence_DoesNotUseBaseMembership(string text)
    {
        var result = KanjiDiagnostics.Analyze(text);
        Assert.False(result.IsJisX0208);
        Assert.False(result.IsJisX0212);
        Assert.False(result.IsJisX0213Plane1);
        Assert.False(result.IsJisX0213Plane2);
        Assert.False(result.IsJisX0213);
        Assert.False(result.IsJoyo);
        Assert.Null(result.JoyoEntry);
        Assert.False(result.IsEducationKanji);
        Assert.Null(result.EducationGrade);
        Assert.False(result.IsJinmeiyoKanji);
        Assert.False(result.IsNameUsableKanji);
        Assert.Empty(result.ReadingEducation);
        Assert.Same(MjCharacter.Find(text), result.MjMatches);
    }

    [Fact]
    public void Analyze_MjFixture_PreservesCombinedMatchFlagsAndEntry()
    {
        var result = KanjiDiagnostics.Analyze("髙");
        var match = Assert.Single(result.MjMatches, m => m.Entry.MjGlyphName == "MJ028902");
        Assert.Equal(MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CorrespondingUcs, match.MatchKind);
        Assert.Same(MjCharacter.GetByMjGlyphName("MJ028902"), match.Entry);
    }

    [Fact]
    public void Analyze_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => KanjiDiagnostics.Analyze((string)null!));

    [Theory]
    [InlineData("")]
    [InlineData("高高")]
    [InlineData("辻\U000E01EF")]
    [InlineData("\uD800")]
    [InlineData("\uDC00")]
    [InlineData("A")]
    [InlineData("あ")]
    public void Analyze_InvalidString_PreservesParseFormatException(string text)
    {
        var expected = Assert.Throws<FormatException>(() => KanjiCharacter.Parse(text));
        var actual = Assert.Throws<FormatException>(() => KanjiDiagnostics.Analyze(text));
        Assert.Equal(expected.Message, actual.Message);
    }
}
