// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JoyoKanjiAppendixTests
{
    [Fact]
    public void GetAll_HasExpectedAppendixDistribution()
    {
        var all = JoyoKanjiAppendix.GetAll();
        var first = JoyoKanjiAppendix.GetByKind(JoyoKanjiAppendixKind.SpecialReading);
        var second = JoyoKanjiAppendix.GetByKind(JoyoKanjiAppendixKind.PrefectureName);
        Assert.Same(all, JoyoKanjiAppendix.GetAll());
        Assert.Equal(128, all.Count);
        Assert.Equal(116, first.Count);
        Assert.Equal(123, first.Sum(entry => entry.Words.Count));
        Assert.Equal(12, second.Count);
        Assert.Equal(33, first.Count(entry => entry.Stage == SchoolStage.Elementary));
        Assert.Equal(53, first.Count(entry => entry.Stage == SchoolStage.JuniorHigh));
        Assert.Equal(30, first.Count(entry => entry.Stage == SchoolStage.HighSchool));
        Assert.All(second, entry => Assert.Equal(SchoolStage.Elementary, entry.Stage));
        Assert.Equal(45, JoyoKanjiAppendix.GetByStage(SchoolStage.Elementary).Count);
        Assert.Equal(53, JoyoKanjiAppendix.GetByStage(SchoolStage.JuniorHigh).Count);
        Assert.Equal(30, JoyoKanjiAppendix.GetByStage(SchoolStage.HighSchool).Count);
        Assert.Same(first, JoyoKanjiAppendix.GetByKind(JoyoKanjiAppendixKind.SpecialReading));
    }

    [Theory]
    [InlineData("明日", "あす", SchoolStage.Elementary)]
    [InlineData("竹刀", "しない", SchoolStage.JuniorHigh)]
    [InlineData("神楽", "かぐら", SchoolStage.HighSchool)]
    [InlineData("時計", "とけい", SchoolStage.Elementary)]
    public void FindByWord_SourceExamples_ReturnExpected(string word, string reading, SchoolStage stage)
    {
        var entry = Assert.Single(JoyoKanjiAppendix.FindByWord(word));
        Assert.Equal(reading, entry.Reading);
        Assert.Equal(stage, entry.Stage);
        Assert.Equal(JoyoKanjiAppendixKind.SpecialReading, entry.Kind);
    }

    [Fact]
    public void AlternativeSpellings_ShareOneEntry()
    {
        var ama = Assert.Single(JoyoKanjiAppendix.FindByWord("海女"));
        Assert.Same(ama, Assert.Single(JoyoKanjiAppendix.FindByWord("海士")));
        Assert.Equal(new[] { "海女", "海士" }, ama.Words);
        Assert.Equal("あま", ama.Reading);
        Assert.Equal(SchoolStage.HighSchool, ama.Stage);
    }

    [Fact]
    public void Shiwasu_NoteIsNotExpandedIntoSecondReading()
    {
        var entry = Assert.Single(JoyoKanjiAppendix.FindByWord("師走"));
        Assert.Equal("しわす", entry.Reading);
        Assert.Equal("（「しはす」とも言う。）", entry.Note);
        Assert.DoesNotContain(entry, JoyoKanjiAppendix.FindByReading("しはす"));
    }

    [Theory]
    [InlineData("愛媛", "えひめ")]
    [InlineData("茨城", "いばらき")]
    [InlineData("奈良", "なら")]
    public void PrefectureNames_AreInSecondAppendix(string word, string reading)
    {
        var entry = Assert.Single(JoyoKanjiAppendix.FindByWord(word));
        Assert.Equal(JoyoKanjiAppendixKind.PrefectureName, entry.Kind);
        Assert.Equal(SchoolStage.Elementary, entry.Stage);
        Assert.Equal(reading, entry.Reading);
    }

    [Fact]
    public void FindByReading_NormalizesKanaAndNfcButRequiresExactMatch()
    {
        var original = Assert.Single(JoyoKanjiAppendix.FindByReading("かぐら"));
        Assert.Contains("神楽", original.Words);
        Assert.Same(original, Assert.Single(JoyoKanjiAppendix.FindByReading("カグラ")));
        Assert.Same(original, Assert.Single(JoyoKanjiAppendix.FindByReading("かぐら")));
        Assert.Empty(JoyoKanjiAppendix.FindByReading("かぐ"));
        Assert.Empty(JoyoKanjiAppendix.FindByWord("神"));
        Assert.Empty(JoyoKanjiAppendix.FindByWord(""));
        Assert.Empty(JoyoKanjiAppendix.FindByReading(""));
    }

    [Fact]
    public void InvalidInputAndEnums_AreRejectedConsistently()
    {
        Assert.Throws<ArgumentNullException>(() => JoyoKanjiAppendix.FindByWord(null!));
        Assert.Throws<ArgumentNullException>(() => JoyoKanjiAppendix.FindByReading(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => JoyoKanjiAppendix.GetByStage((SchoolStage)99));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            JoyoKanjiAppendix.GetByKind((JoyoKanjiAppendixKind)99));
    }
}
