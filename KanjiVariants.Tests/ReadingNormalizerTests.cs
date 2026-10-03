// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class ReadingNormalizerTests
{
    [Theory]
    [InlineData("かんじ")]
    [InlineData("せい")]
    [InlineData("ABC123")]
    [InlineData("abcかな")]
    [InlineData("ｶﾝｼﾞ")]
    [InlineData("")]
    public void Normalize_UnchangedReading_ReturnsSameInstance(string value)
    {
        // インターン済み文字列の偶然の一致を避け、内容と再利用の両方を確認します。
        string input = new(value.ToCharArray());
        string result = ReadingNormalizer.Normalize(input);
        Assert.Equal(value, result);
        Assert.Same(input, result);
    }

    [Theory]
    [InlineData("カンジ", "かんじ")]
    [InlineData("スーパー", "すーぱー")]
    [InlineData("き\u3099ん", "ぎん")]
    [InlineData("キ\u3099ン", "ぎん")]
    public void Normalize_KatakanaOrDecomposedDakuten_ReturnsNormalizedHiragana(
        string input, string expected) => Assert.Equal(expected, ReadingNormalizer.Normalize(input));
}
