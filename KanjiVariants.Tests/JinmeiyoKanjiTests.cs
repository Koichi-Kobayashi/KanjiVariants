// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JinmeiyoKanjiTests
{
    // いずれもMJ文字情報一覧表で「漢字施策 = 人名用漢字」の行から選んでいます。
    [Theory]
    [InlineData("丑")]
    [InlineData("丞")]
    [InlineData("乃")]
    [InlineData("亞")]
    public void IsJinmeiyoKanji_SourceFixture_ReturnsTrue(string character)
    {
        Assert.True(JinmeiyoKanji.IsJinmeiyoKanji(character));
        Assert.True(JinmeiyoKanji.IsJinmeiyoKanji(KanjiCharacter.Parse(character)));
        Assert.True(JinmeiyoKanji.IsNameUsableKanji(character));
    }

    [Fact]
    public void IsNameUsableKanji_JoyoOnlyCharacter_ReturnsTrue()
    {
        Assert.True(JoyoKanji.IsJoyo("高"));
        Assert.False(JinmeiyoKanji.IsJinmeiyoKanji("高"));
        Assert.True(JinmeiyoKanji.IsNameUsableKanji("高"));
        Assert.True(JinmeiyoKanji.IsNameUsableKanji(KanjiCharacter.Parse("高")));
    }

    [Theory]
    [InlineData("あ")]
    [InlineData("ア")]
    [InlineData("A")]
    [InlineData("丑丞")]
    [InlineData("")]
    [InlineData("\uD800")]
    [InlineData("丑\U000E0101")]
    [InlineData("欄\uFE00")]
    public void Membership_UnsupportedInput_ReturnsFalse(string character)
    {
        Assert.False(JinmeiyoKanji.IsJinmeiyoKanji(character));
        Assert.False(JinmeiyoKanji.IsNameUsableKanji(character));
    }

    [Fact]
    public void Membership_Null_ReturnsFalse()
    {
        Assert.False(JinmeiyoKanji.IsJinmeiyoKanji((string?)null));
        Assert.False(JinmeiyoKanji.IsNameUsableKanji((string?)null));
    }

    [Fact]
    public void Membership_KanjiCharacterWithVariationSelector_DoesNotFallback()
    {
        var ivs = KanjiCharacter.Parse("丑\U000E0101");
        Assert.False(JinmeiyoKanji.IsJinmeiyoKanji(ivs));
        Assert.False(JinmeiyoKanji.IsNameUsableKanji(ivs));
    }

    [Fact]
    public void GetAll_Returns863DistinctSharedReadOnlyCharacters()
    {
        var all = JinmeiyoKanji.GetAll();
        Assert.Same(all, JinmeiyoKanji.GetAll());
        Assert.Equal(863, all.Count);
        Assert.Equal(863, all.Select(x => x.BaseCharacter.Value).Distinct().Count());
        Assert.All(all, character =>
        {
            Assert.False(character.HasVariationSelector);
            Assert.True(JinmeiyoKanji.IsJinmeiyoKanji(character));
            Assert.True(JinmeiyoKanji.IsJinmeiyoKanji(character.ToString()));
            Assert.True(JinmeiyoKanji.IsNameUsableKanji(character));
            Assert.False(JoyoKanji.IsJoyo(character));
        });
        Assert.Throws<NotSupportedException>(() =>
            ((IList<KanjiCharacter>)all).Add(KanjiCharacter.Parse("高")));
    }

    [Fact]
    public void IsNameUsableKanji_AgreesWithJoyoOrJinmeiyoForWholeSets()
    {
        // 常用漢字には補助平面の「𠮟」もあるため、BMP走査に加えて確認します。
        int joyoCount = 0;
        for (int cp = 0x3400; cp <= 0xFAFF; cp++)
        {
            if (!System.Text.Rune.IsValid(cp))
                continue;
            string text = char.ConvertFromUtf32(cp);
            bool joyo = JoyoKanji.IsJoyo(text);
            bool jinmeiyo = JinmeiyoKanji.IsJinmeiyoKanji(text);
            Assert.Equal(joyo || jinmeiyo, JinmeiyoKanji.IsNameUsableKanji(text));
            if (joyo)
                joyoCount++;
        }
        Assert.True(JoyoKanji.IsJoyo("𠮟"));
        Assert.Equal(JoyoKanji.IsJoyo("𠮟") || JinmeiyoKanji.IsJinmeiyoKanji("𠮟"),
            JinmeiyoKanji.IsNameUsableKanji("𠮟"));
        joyoCount++;
        Assert.Equal(2136, joyoCount);
        Assert.All(JinmeiyoKanji.GetAll(), character =>
            Assert.Equal(JoyoKanji.IsJoyo(character) || JinmeiyoKanji.IsJinmeiyoKanji(character),
                JinmeiyoKanji.IsNameUsableKanji(character)));
    }
}
