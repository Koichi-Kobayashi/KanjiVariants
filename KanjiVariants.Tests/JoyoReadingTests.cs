// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class JoyoReadingTests
{
    // 固定HTMLと生成データで確認した掲載音訓を使っています。
    [Theory]
    [InlineData("高", "コウ")]
    [InlineData("高", "こう")]
    [InlineData("高", "たかい")]
    [InlineData("愛", "アイ")]
    [InlineData("愛", "あい")]
    [InlineData("銀", "ギン")]
    [InlineData("銀", "き\u3099ん")]
    [InlineData("銀", "キ\u3099ン")]
    [InlineData("泳", "およぐ")]
    [InlineData("𠮟", "しかる")]
    public void ListedReading_SourceFixture_IsListedAndNotHyogai(string character, string reading)
    {
        Assert.True(JoyoReading.IsListedReading(character, reading));
        Assert.False(JoyoReading.IsHyogaiReading(character, reading));
        var parsed = KanjiCharacter.Parse(character);
        Assert.True(JoyoReading.IsListedReading(parsed, reading));
        Assert.False(JoyoReading.IsHyogaiReading(parsed, reading));
    }

    [Theory]
    [InlineData("愛", "いとしい")]
    [InlineData("高", "こ")]
    [InlineData("高", "こうこう")]
    [InlineData("高", "コー")]
    [InlineData("高", "こう ")]
    [InlineData("高", "たかった")]
    [InlineData("泳", "およいだ")]
    [InlineData("愛", "")]
    public void UnlistedReading_JoyoCharacter_IsHyogaiWithoutInferringForms(string character, string reading)
    {
        Assert.True(JoyoKanji.IsJoyo(character));
        Assert.False(JoyoKanji.IsReadingSupported(character, reading));
        Assert.False(JoyoReading.IsListedReading(character, reading));
        Assert.True(JoyoReading.IsHyogaiReading(character, reading));
        var parsed = KanjiCharacter.Parse(character);
        Assert.False(JoyoReading.IsListedReading(parsed, reading));
        Assert.True(JoyoReading.IsHyogaiReading(parsed, reading));
    }

    [Theory]
    [InlineData(null, "こう")]
    [InlineData("", "こう")]
    [InlineData("高愛", "こう")]
    [InlineData("あ", "あ")]
    [InlineData("A", "あ")]
    [InlineData("\uD800", "あ")]
    [InlineData("丑", "うし")]
    [InlineData("辻", "つじ")]
    [InlineData("亞", "あ")]
    [InlineData("髙", "こう")]
    [InlineData("亜\U000E0100", "あ")]
    [InlineData("不\uFE00", "ふ")]
    [InlineData(null, "")]
    public void UnsupportedCharacter_NeitherListedNorHyogai(string? character, string reading)
    {
        Assert.False(JoyoReading.IsListedReading(character, reading));
        Assert.False(JoyoReading.IsHyogaiReading(character, reading));
    }

    [Theory]
    [InlineData("丑", "うし")]
    [InlineData("亞", "あ")]
    [InlineData("髙", "こう")]
    [InlineData("亜\U000E0100", "あ")]
    [InlineData("不\uFE00", "ふ")]
    public void KanjiCharacterOverloads_DoNotConvertVariantsOrDiscardSelectors(string text, string reading)
    {
        var character = KanjiCharacter.Parse(text);
        Assert.False(JoyoReading.IsListedReading(character, reading));
        Assert.False(JoyoReading.IsHyogaiReading(character, reading));
    }

    [Theory]
    [InlineData("高")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    public void NullReading_ThrowsEvenWhenCharacterIsInvalid(string? character)
    {
        Assert.Throws<ArgumentNullException>(() => JoyoReading.IsListedReading(character, null!));
        Assert.Throws<ArgumentNullException>(() => JoyoReading.IsHyogaiReading(character, null!));
    }

    [Fact]
    public void KanjiCharacterOverloads_DefaultAndNullReading_FollowExistingPolicy()
    {
        Assert.False(JoyoReading.IsListedReading(default(KanjiCharacter), "こう"));
        Assert.False(JoyoReading.IsHyogaiReading(default(KanjiCharacter), "こう"));
        foreach (var character in new[] { default(KanjiCharacter), KanjiCharacter.Parse("高") })
        {
            Assert.Throws<ArgumentNullException>(() => JoyoReading.IsListedReading(character, null!));
            Assert.Throws<ArgumentNullException>(() => JoyoReading.IsHyogaiReading(character, null!));
        }
    }

    [Fact]
    public void WholeJoyoCorpus_All4388ReadingsAreListedAndHyogaiAgreesWithDefinition()
    {
        // 本表字をBMPの漢字範囲と補助平面の「𠮟」から取得します。無効なscalarは除きます。
        var entries = Enumerable.Range(0x3400, 0xFAFF - 0x3400 + 1)
            .Where(Rune.IsValid)
            .Select(char.ConvertFromUtf32)
            .Append("𠮟")
            .Select(JoyoKanji.Get)
            .OfType<JoyoKanjiEntry>()
            .ToArray();
        Assert.Equal(2136, entries.Length);
        Assert.Equal(4388, entries.Sum(entry => entry.Readings.Count));
        foreach (var entry in entries)
        {
            foreach (var reading in entry.Readings)
            {
                Assert.True(JoyoReading.IsListedReading(entry.Character, reading.Reading));
                Assert.True(JoyoReading.IsListedReading(entry.Character.ToString(), reading.Reading));
                Assert.False(JoyoReading.IsHyogaiReading(entry.Character, reading.Reading));
                Assert.False(JoyoReading.IsHyogaiReading(entry.Character.ToString(), reading.Reading));
            }
            // 未掲載の入力と空読みについても、両オーバーロードの定義を全本表字で確認します。
            foreach (string reading in new[] { "みとうろくのよみ", "" })
            {
                Assert.False(JoyoReading.IsListedReading(entry.Character, reading));
                Assert.Equal(JoyoKanji.IsJoyo(entry.Character) && !JoyoReading.IsListedReading(entry.Character, reading),
                    JoyoReading.IsHyogaiReading(entry.Character, reading));
                Assert.Equal(JoyoReading.IsHyogaiReading(entry.Character, reading),
                    JoyoReading.IsHyogaiReading(entry.Character.ToString(), reading));
            }
        }
    }
}
