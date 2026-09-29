// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class EducationKanjiTests
{
    [Theory]
    [InlineData("学", true)]
    [InlineData("茨", true)]
    [InlineData("學", false)]
    [InlineData("髙", false)]
    [InlineData("学\U000E0100", false)]
    [InlineData("不\uFE00", false)]
    [InlineData("A", false)]
    [InlineData("あ", false)]
    [InlineData("学校", false)]
    [InlineData("", false)]
    public void IsEducationKanji_OnlyListedBaseCharacterIsTrue(string character, bool expected) =>
        Assert.Equal(expected, EducationKanji.IsEducationKanji(character));

    [Theory]
    [InlineData("学", KanjiGrade.Grade1)]
    [InlineData("高", KanjiGrade.Grade2)]
    [InlineData("漢", KanjiGrade.Grade3)]
    [InlineData("茨", KanjiGrade.Grade4)]
    [InlineData("囲", KanjiGrade.Grade5)]
    [InlineData("胃", KanjiGrade.Grade6)]
    public void GetGrade_OfficialFixedFixture_ReturnsExpectedGrade(string character, KanjiGrade expected) =>
        Assert.Equal(expected, EducationKanji.GetGrade(character));

    [Theory]
    [InlineData("學")]
    [InlineData("髙")]
    [InlineData("学\U000E0100")]
    [InlineData("不\uFE00")]
    [InlineData("学校")]
    [InlineData("")]
    public void GetGrade_OutsideTable_ReturnsNull(string character) =>
        Assert.Null(EducationKanji.GetGrade(character));

    [Theory]
    [InlineData(KanjiGrade.Grade1, 80, "一", "六")]
    [InlineData(KanjiGrade.Grade2, 160, "引", "話")]
    [InlineData(KanjiGrade.Grade3, 200, "悪", "和")]
    [InlineData(KanjiGrade.Grade4, 202, "愛", "録")]
    [InlineData(KanjiGrade.Grade5, 193, "圧", "歴")]
    [InlineData(KanjiGrade.Grade6, 191, "胃", "論")]
    public void GetByGrade_PreservesCountAndOriginalOrder(KanjiGrade grade, int count, string first, string last)
    {
        var characters = EducationKanji.GetByGrade(grade);
        Assert.Equal(count, characters.Count);
        Assert.Equal(first, characters[0].ToString());
        Assert.Equal(last, characters[^1].ToString());
        Assert.Same(characters, EducationKanji.GetByGrade(grade));
        Assert.Throws<NotSupportedException>(() => ((IList<KanjiCharacter>)characters).Add(KanjiCharacter.Parse("学")));
    }

    [Fact]
    public void GetByGrade_AllGradesHave1026DistinctCharactersAndConsistentMembership()
    {
        var values = Enumerable.Range(1, 6)
            .SelectMany(grade => EducationKanji.GetByGrade((KanjiGrade)grade))
            .ToArray();
        Assert.Equal(1026, values.Length);
        Assert.Equal(1026, values.Distinct().Count());
        foreach (var grade in Enumerable.Range(1, 6))
            Assert.All(EducationKanji.GetByGrade((KanjiGrade)grade),
                character => Assert.Equal((KanjiGrade)grade, EducationKanji.GetGrade(character)));
    }

    [Fact]
    public void GetByGrade_InvalidGrade_ThrowsArgumentOutOfRangeException() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => EducationKanji.GetByGrade((KanjiGrade)99));

    [Fact]
    public void KanjiCharacterOverloads_DoNotDiscardVariationSelector()
    {
        var plain = KanjiCharacter.Parse("学");
        var ivs = KanjiCharacter.Parse("学\U000E0100");
        Assert.True(EducationKanji.IsEducationKanji(plain));
        Assert.Equal(KanjiGrade.Grade1, EducationKanji.GetGrade(plain));
        Assert.False(EducationKanji.IsEducationKanji(ivs));
        Assert.Null(EducationKanji.GetGrade(ivs));
    }
}
