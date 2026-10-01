// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JoyoKanjiTests
{
    [Theory]
    [InlineData("亜", true)]
    [InlineData("高", true)]
    [InlineData("亞", false)]
    [InlineData("髙", false)]
    [InlineData("辻", false)]
    [InlineData("亜\U000E0100", false)]
    [InlineData("A", false)]
    [InlineData("", false)]
    public void IsJoyo_ReturnsExpectedForMainFormOnly(string value, bool expected) =>
        Assert.Equal(expected, JoyoKanji.IsJoyo(value));

    [Fact]
    public void Get_OldFormIsRecordedButNotReverseLookedUp()
    {
        var entry = Assert.IsType<JoyoKanjiEntry>(JoyoKanji.Get("亜"));
        Assert.Equal("亜", entry.Character.ToString());
        Assert.Equal("亞", entry.OldForm?.ToString());
        Assert.Equal(new[] { "亞" }, entry.OldForms.Select(x => x.ToString()));
        Assert.Null(JoyoKanji.Get("亞"));
        Assert.Null(JoyoKanji.Get("髙"));
        Assert.Null(JoyoKanji.Get("亜\U000E0100"));
    }

    [Fact]
    public void Get_MultipleOldFormsArePreservedWithoutSelectingOne()
    {
        var entry = Assert.IsType<JoyoKanjiEntry>(JoyoKanji.Get("弁"));
        Assert.Null(entry.OldForm);
        Assert.Equal(new[] { "辨", "瓣", "辯" }, entry.OldForms.Select(x => x.ToString()));
        Assert.Equal("弁（辨）（瓣）（辯）", entry.SourceLabel);
        Assert.Null(JoyoKanji.Get("辨"));
    }

    [Fact]
    public void Get_BracketedGlyphAnnotationIsPreserved()
    {
        var entry = Assert.IsType<JoyoKanjiEntry>(JoyoKanji.Get("餅"));
        Assert.Equal("餅［餅］（餠）", entry.SourceLabel);
        Assert.Equal("餠", entry.OldForm?.ToString());
        Assert.Contains("許容字体", entry.Readings[0].Note);
    }

    [Fact]
    public void GetReadings_PreservesOnKunExamplesAndOriginalNotation()
    {
        var readings = JoyoKanji.GetReadings("哀");
        Assert.Equal(3, readings.Count);
        Assert.Equal(KanjiReadingType.On, readings[0].Type);
        Assert.Equal("アイ", readings[0].Reading);
        Assert.Equal(new[] { "哀愁", "哀願", "悲哀" }, readings[0].Examples);
        Assert.Equal(KanjiReadingType.Kun, readings[1].Type);
        Assert.Equal("あわれ", readings[1].Reading);
        Assert.Equal("あわれむ", readings[2].Reading);
        Assert.Empty(JoyoKanji.GetReadings("髙"));
    }

    [Fact]
    public void GetReadings_PreservesNotesAndWrappedExamples()
    {
        var ai = Assert.Single(JoyoKanji.GetReadings("愛"));
        Assert.Equal("愛媛（えひめ）県", ai.Note);
        var ha = Assert.Single(JoyoKanji.GetReadings("把"));
        Assert.Equal(new[] { "把握", "把持", "一把（ワ）", "三把（バ）", "十把（パ）" }, ha.Examples);
        var hane = Assert.Single(JoyoKanji.GetReadings("羽"), x => x.Reading == "はね");
        Assert.Equal(new[] { "羽", "羽飾り" }, hane.Examples);
        var kiwameru = Assert.Single(JoyoKanji.GetReadings("極"), x => x.Reading == "きわめる");
        Assert.Contains("極めて〔副〕", kiwameru.Examples);
        var ma = Assert.Single(JoyoKanji.GetReadings("真"), x => x.Reading == "ま");
        Assert.Contains("真ん中", ma.Examples);
    }

    [Fact]
    public void FindByReading_NormalizesKanaButRetainsSourceReading()
    {
        var katakana = JoyoKanji.FindByReading("コウ");
        var hiragana = JoyoKanji.FindByReading("こう");
        Assert.Equal(katakana.Select(x => x.Character), hiragana.Select(x => x.Character));
        var high = Assert.Single(katakana, x => x.Character.ToString() == "高");
        Assert.Contains(high.Readings, x => x.Reading == "コウ" && x.Type == KanjiReadingType.On);
    }

    [Fact]
    public void FindByReading_TypeFilterAndExactMatchWork()
    {
        Assert.Contains(JoyoKanji.FindByReading("あわれ", KanjiReadingType.Kun), x => x.Character.ToString() == "哀");
        Assert.DoesNotContain(JoyoKanji.FindByReading("あわれ", KanjiReadingType.On), x => x.Character.ToString() == "哀");
        Assert.Contains(JoyoKanji.FindByReading("こう", KanjiReadingType.On), x => x.Character.ToString() == "高");
        Assert.DoesNotContain(JoyoKanji.FindByReading("こ", KanjiReadingType.On), x => x.Character.ToString() == "高");
        Assert.Empty(JoyoKanji.FindByReading("みとうろくのよみ"));
    }

    [Theory]
    [InlineData("高", "コウ", true)]
    [InlineData("高", "こう", true)]
    [InlineData("高", "たかい", true)]
    [InlineData("高", "こ", false)]
    [InlineData("高", "アイ", false)]
    [InlineData("髙", "コウ", false)]
    public void IsReadingSupported_UsesNormalizedExactMatch(string character, string reading, bool expected) =>
        Assert.Equal(expected, JoyoKanji.IsReadingSupported(character, reading));

    [Theory]
    [InlineData("がく", true)]
    [InlineData("ガク", true)]
    [InlineData("か\u3099く", true)]
    [InlineData("カ\u3099ク", true)]
    [InlineData("がク", true)]
    [InlineData("か\u3099くい", false)]
    public void ReadingQueries_NfcAndKanaVariants_PreserveExactMatch(string reading, bool expected)
    {
        Assert.Equal(expected, JoyoKanji.IsReadingSupported("学", reading));
        Assert.Equal(expected, JoyoKanji.FindByReading(reading).Any(x => x.Character.ToString() == "学"));
        var education = JoyoKanjiEducation.GetReading("学", reading);
        Assert.Equal(expected, education is not null);
        if (expected)
            Assert.Equal("ガク", education!.Reading.Reading);
    }

    [Fact]
    public void ReadingQueries_InvalidUtf16_StillThrowsArgumentException()
    {
        // テストケースの転送時に不正な符号単位が置換されないよう、実行時に作ります。
        foreach (char surrogate in new[] { '\uD800', '\uDC00' })
        {
            string reading = new string(surrogate, 1);
            Assert.Throws<ArgumentException>(() => JoyoKanji.FindByReading(reading));
            Assert.Throws<ArgumentException>(() => JoyoKanji.IsReadingSupported("学", reading));
            Assert.Throws<ArgumentException>(() => JoyoKanjiEducation.GetReading("学", reading));
        }
    }

    [Fact]
    public void KanjiCharacterOverloads_UseTheSameEntry()
    {
        var character = KanjiCharacter.Parse("亜");
        Assert.True(JoyoKanji.IsJoyo(character));
        Assert.Same(JoyoKanji.Get("亜"), JoyoKanji.Get(character));
        Assert.Same(JoyoKanji.GetReadings("亜"), JoyoKanji.GetReadings(character));
        Assert.True(JoyoKanji.IsReadingSupported(character, "あ"));
    }

    [Fact]
    public void InvalidInput_ReturnsEmptyResults()
    {
        Assert.False(JoyoKanji.IsJoyo("あ"));
        Assert.Null(JoyoKanji.Get("あ"));
        Assert.Empty(JoyoKanji.GetReadings("あ"));
        Assert.False(JoyoKanji.IsReadingSupported("あ", "あ"));
        Assert.Throws<ArgumentOutOfRangeException>(() => JoyoKanji.FindByReading("あ", (KanjiReadingType)99));
    }
}
