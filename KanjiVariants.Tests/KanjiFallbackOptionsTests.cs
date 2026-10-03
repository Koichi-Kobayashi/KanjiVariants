// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class KanjiFallbackOptionsTests
{
    [Theory]
    [InlineData(0x4000)]
    [InlineData(0x4001)]
    [InlineData(-1)]
    public void UndefinedBits_AllPublicOverloads_ThrowForOptions(int value)
    {
        var options = (KanjiFallbackOptions)value;
        var character = KanjiCharacter.Parse("辻\U000E0100");
        const CharacterSet set = CharacterSet.JisX0208;
        // 正常な文字入力を使い、解析エラーより後のoptions検証を確認します。
        Assert.Throws<ArgumentOutOfRangeException>("options", () => Kanji.GetAlternatives(character, set, options));
        Assert.Throws<ArgumentOutOfRangeException>("options", () => Kanji.GetAlternatives(character.ToString(), set, options));
        Assert.Throws<ArgumentOutOfRangeException>("options", () => Kanji.TryGetAlternative(character, set, out _, options));
        Assert.Throws<ArgumentOutOfRangeException>("options", () => Kanji.TryGetAlternative(character.ToString(), set, out _, options));
        // 空文字でも検証を省略しないことを固定します。
        Assert.Throws<ArgumentOutOfRangeException>("options", () => KanjiText.Replace("", set, options));
        Assert.Throws<ArgumentOutOfRangeException>("options", () => KanjiText.Replace(character.ToString(), set, options));
    }

    // Noneと唯一の定義済みbitの正常動作は、既存のIVS/SVSテストで確認しています。
}
