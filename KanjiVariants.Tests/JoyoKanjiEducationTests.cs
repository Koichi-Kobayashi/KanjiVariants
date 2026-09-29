// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JoyoKanjiEducationTests
{
    [Theory]
    [InlineData("衣", "イ", SchoolStage.Elementary)]
    [InlineData("衣", "ころも", SchoolStage.JuniorHigh)]
    [InlineData("悪", "アク", SchoolStage.Elementary)]
    [InlineData("悪", "オ", SchoolStage.HighSchool)]
    [InlineData("宮", "キュウ", SchoolStage.Elementary)]
    [InlineData("宮", "グウ", SchoolStage.JuniorHigh)]
    [InlineData("宮", "ク", SchoolStage.HighSchool)]
    [InlineData("𠮟", "シツ", SchoolStage.JuniorHigh)]
    [InlineData("𠮟", "しかる", SchoolStage.JuniorHigh)]
    public void GetReadingStage_SourceExamples_ReturnExpected(
        string character, string reading, SchoolStage expected) =>
        Assert.Equal(expected, JoyoKanjiEducation.GetReadingStage(character, reading));

    [Fact]
    public void GradeAndReadingStage_AreIndependent()
    {
        Assert.Equal(KanjiGrade.Grade4, EducationKanji.GetGrade("衣"));
        Assert.Equal(SchoolStage.Elementary, JoyoKanjiEducation.GetReadingStage("衣", "イ"));
        Assert.Equal(SchoolStage.JuniorHigh, JoyoKanjiEducation.GetReadingStage("衣", "ころも"));
        Assert.Equal(KanjiGrade.Grade3, EducationKanji.GetGrade("悪"));
        Assert.Equal(SchoolStage.Elementary, JoyoKanjiEducation.GetReadingStage("悪", "アク"));
        Assert.Equal(SchoolStage.HighSchool, JoyoKanjiEducation.GetReadingStage("悪", "オ"));
    }

    [Theory]
    [InlineData("亞")]
    [InlineData("髙")]
    [InlineData("辻")]
    [InlineData("高\U000E0100")]
    [InlineData("亜\U000E0100")]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("A")]
    [InlineData("\uD800")]
    public void GetReadings_NonMainForm_ReturnsSharedEmpty(string character)
    {
        var result = JoyoKanjiEducation.GetReadings(character);
        Assert.Empty(result);
        Assert.Same(result, JoyoKanjiEducation.GetReadings("不存在"));
        Assert.Null(JoyoKanjiEducation.GetReading(character, "ア"));
        Assert.Null(JoyoKanjiEducation.GetReadingStage(character, "ア"));
        Assert.False(JoyoKanjiEducation.IsReadingAssigned(character, "ア", SchoolStage.Elementary));
    }

    [Fact]
    public void CharacterOverload_UsesSameResults()
    {
        var character = KanjiCharacter.Parse("宮");
        Assert.Same(JoyoKanjiEducation.GetReadings(character), JoyoKanjiEducation.GetReadings("宮"));
        Assert.Same(JoyoKanjiEducation.GetReading(character, "ク"), JoyoKanjiEducation.GetReading("宮", "ク"));
        Assert.True(JoyoKanjiEducation.IsReadingAssigned(character, "ク", SchoolStage.HighSchool));
    }

    [Fact]
    public void GetReading_NormalizesKanaAndNfcButRequiresExactMatch()
    {
        var source = JoyoKanjiEducation.GetReading("高", "コウ");
        Assert.NotNull(source);
        Assert.Same(source, JoyoKanjiEducation.GetReading("高", "こう"));
        Assert.Equal("コウ", source.Reading.Reading);
        Assert.Null(JoyoKanjiEducation.GetReading("高", "コ"));
        Assert.Null(JoyoKanjiEducation.GetReading("高", "こうい"));
        Assert.Null(JoyoKanjiEducation.GetReading("高", ""));
    }

    [Theory]
    [InlineData("依", "エ", true)]
    [InlineData("遺", "ユイ", true)]
    [InlineData("火", "ほ", true)]
    [InlineData("亜", "ア", false)]
    [InlineData("衣", "イ", false)]
    public void IsSpecialOrLimited_ReflectsSourceIndentation(string character, string reading, bool expected)
    {
        var entry = Assert.IsType<KanjiReadingEducation>(JoyoKanjiEducation.GetReading(character, reading));
        Assert.Equal(expected, entry.IsSpecialOrLimited);
    }

    [Fact]
    public void GetReadings_ReusesOriginalReadingAndOriginalOrder()
    {
        var original = JoyoKanji.GetReadings("哀");
        var education = JoyoKanjiEducation.GetReadings("哀");
        Assert.Equal(original.Count, education.Count);
        for (int i = 0; i < original.Count; i++)
            Assert.Same(original[i], education[i].Reading);
        Assert.Same(education, JoyoKanjiEducation.GetReadings("哀"));
    }

    [Fact]
    public void EveryMainReading_MatchesOriginalOnceAndOneStage()
    {
        var seen = new HashSet<KanjiReading>(ReferenceEqualityComparer.Instance);
        int characters = 0;
        int readings = 0;
        for (int cp = 0x3400; cp <= 0x20B9F; cp++)
        {
            if (!Rune.IsValid(cp))
                continue;
            var character = char.ConvertFromUtf32(cp);
            if (!JoyoKanji.IsJoyo(character))
                continue;
            characters++;
            var original = JoyoKanji.GetReadings(character);
            var education = JoyoKanjiEducation.GetReadings(character);
            Assert.Equal(original.Count, education.Count);
            for (int i = 0; i < original.Count; i++)
            {
                Assert.Same(original[i], education[i].Reading);
                Assert.True(seen.Add(education[i].Reading));
                Assert.Equal(education[i].Stage,
                    JoyoKanjiEducation.GetReadingStage(character, original[i].Reading));
                readings++;
            }
        }
        Assert.Equal(2136, characters);
        Assert.Equal(4388, readings);
    }

    [Fact]
    public void GetByStage_HasExpectedDistributionAndSharedLists()
    {
        var elementary = JoyoKanjiEducation.GetByStage(SchoolStage.Elementary);
        var juniorHigh = JoyoKanjiEducation.GetByStage(SchoolStage.JuniorHigh);
        var highSchool = JoyoKanjiEducation.GetByStage(SchoolStage.HighSchool);
        Assert.Equal(2062, elementary.Count);
        Assert.Equal(2008, juniorHigh.Count);
        Assert.Equal(318, highSchool.Count);
        Assert.All(elementary, entry => Assert.Equal(SchoolStage.Elementary, entry.Stage));
        Assert.All(juniorHigh, entry => Assert.Equal(SchoolStage.JuniorHigh, entry.Stage));
        Assert.All(highSchool, entry => Assert.Equal(SchoolStage.HighSchool, entry.Stage));
        Assert.Equal(4388, elementary.Count + juniorHigh.Count + highSchool.Count);
        Assert.Equal(4388, elementary.Concat(juniorHigh).Concat(highSchool).Distinct(ReferenceEqualityComparer.Instance).Count());
        Assert.Equal(178, elementary.Concat(juniorHigh).Concat(highSchool).Count(entry => entry.IsSpecialOrLimited));
        Assert.Same(elementary, JoyoKanjiEducation.GetByStage(SchoolStage.Elementary));
    }

    [Fact]
    public void NullAndInvalidEnum_FollowExistingApiRules()
    {
        Assert.Empty(JoyoKanjiEducation.GetReadings((string?)null));
        Assert.Null(JoyoKanjiEducation.GetReading((string?)null, "ア"));
        Assert.Throws<ArgumentNullException>(() => JoyoKanjiEducation.GetReading("亜", null!));
        Assert.Throws<ArgumentNullException>(() => JoyoKanjiEducation.GetReadingStage("亜", null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => JoyoKanjiEducation.GetByStage((SchoolStage)99));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JoyoKanjiEducation.IsReadingAssigned("亜", "ア", (SchoolStage)99));
    }
}
