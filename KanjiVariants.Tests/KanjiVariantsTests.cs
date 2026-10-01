// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class KanjiVariantsTests
{
    private const CharacterSet JisX0208 = CharacterSet.JisX0208;
    private const CharacterSet Plane1 = CharacterSet.JisX0213Plane1;
    private const CharacterSet Plane2 = CharacterSet.JisX0213Plane2;
    private const CharacterSet BothPlanes = CharacterSet.JisX0213;
    private const KanjiFallbackOptions AllowVs = KanjiFallbackOptions.AllowVariationSelectorFallback;
    private const string TsujiIvs = "辻󠄀";
    private const string SakakiIvs = "榊󠄀";

    [Theory]
    [InlineData("丑", Plane1, true)]
    [InlineData("丑", Plane2, false)]
    [InlineData("丑", BothPlanes, true)]
    [InlineData("高", Plane1, true)]
    [InlineData("高", Plane2, false)]
    [InlineData("\u3406", Plane1, false)]
    [InlineData("\u3406", Plane2, true)]
    [InlineData("\u3406", BothPlanes, true)]
    [InlineData("\u3404", Plane1, false)]
    [InlineData("\u3404", Plane2, false)]
    [InlineData("\u3404", BothPlanes, false)]
    public void IsSupported_CharacterSetMembership_ReturnsExpected(string character, CharacterSet characterSet, bool expected)
    {
        // VSなしの「丑」は、MJ006315の実装したUCSに従い第1面だけに所属します。
        Assert.Equal(expected, Kanji.IsSupported(character, characterSet));
    }

    [Theory]
    [InlineData("高橋吉野", JisX0208, true)]
    [InlineData("髙橋𠮷野", JisX0208, false)]
    [InlineData("①", JisX0208, false)]
    [InlineData("ABC123", JisX0208, true)]
    [InlineData("", JisX0208, true)]
    [InlineData("ABC\u3406", Plane1, false)]
    [InlineData("ABC\u3406", Plane2, true)]
    [InlineData("辻\U000E0100", JisX0208, false)]
    [InlineData("辻\U000E0100", BothPlanes, false)]
    [InlineData("高\uFE0F", JisX0208, false)]
    [InlineData("高\uFE0F", Plane1, false)]
    [InlineData("高\uFE0F", BothPlanes, false)]
    [InlineData("高\ud800", JisX0208, false)]
    public void TextIsSupported_ReturnsExpected(string text, CharacterSet characterSet, bool expected) =>
        Assert.Equal(expected, KanjiText.IsSupported(text, characterSet));

    [Theory]
    [InlineData("髙", "高")]
    [InlineData("𠮷", "吉")]
    public void GetAlternatives_JisX0208_ContainsMjCandidate(string source, string expected) =>
        Assert.Contains(expected, Kanji.GetAlternatives(source, JisX0208).Select(x => x.ToString()));

    [Fact]
    public void GetAlternatives_JisX0208_MultipleCandidatesAreDistinctAndSupported()
    {
        // U+3404にはJIS X 0208に属する候補U+725BとU+4E95があります。
        const string source = "\u3404";
        var candidates = Kanji.GetAlternatives(source, JisX0208);

        Assert.True(candidates.Count >= 2);
        Assert.Equal(candidates.Count, candidates.Select(x => x.ToString()).Distinct().Count());
        Assert.All(candidates, candidate => Assert.True(Kanji.IsSupported(candidate, JisX0208)));
    }

    [Fact]
    public void GetAlternatives_JisX0213_FiltersCandidatesByPlaneWithoutDuplication()
    {
        // MJ000040の候補は第1面のU+8846と第2面のU+4E51です。
        const string source = "\u343A";
        Assert.Equal(new[] { "\u8846" }, Kanji.GetAlternatives(source, Plane1).Select(x => x.ToString()));
        Assert.Equal(new[] { "\u4E51" }, Kanji.GetAlternatives(source, Plane2).Select(x => x.ToString()));
        var combined = Kanji.GetAlternatives(source, BothPlanes).Select(x => x.ToString()).ToArray();
        Assert.Equal(2, combined.Length);
        Assert.Contains("\u8846", combined);
        Assert.Contains("\u4E51", combined);
        Assert.Equal(combined.Length, combined.Distinct().Count());
    }

    [Theory]
    [InlineData("髙", JisX0208, "高")]
    [InlineData("𠮷", JisX0208, "吉")]
    [InlineData("\u343A", Plane2, "\u4E51")]
    [InlineData("\u343A", BothPlanes, "\u4E51")]
    public void TryGetAlternative_MjUniqueTarget_ReturnsExpected(string source, CharacterSet characterSet, string expected)
    {
        Assert.True(Kanji.TryGetAlternative(source, characterSet, out var alternative));
        Assert.Equal(expected, alternative.ToString());
    }

    [Theory]
    [InlineData("〻", JisX0208)]
    [InlineData("\u343A", Plane1)]
    public void TryGetAlternative_TargetOutsideCharacterSet_ReturnsFalse(string source, CharacterSet characterSet) =>
        Assert.False(Kanji.TryGetAlternative(source, characterSet, out _));

    [Theory]
    [InlineData("〻", JisX0208, "〻")]
    [InlineData("髙橋𠮷野", JisX0208, "高橋吉野")]
    [InlineData("\u343A", Plane1, "\u343A")]
    [InlineData("\u343A", Plane2, "\u4E51")]
    [InlineData("\u343A", BothPlanes, "\u4E51")]
    [InlineData("亟", JisX0208, "亟")]
    [InlineData("", JisX0208, "")]
    public void Replace_MjUniqueMapping_ReturnsExpected(string source, CharacterSet characterSet, string expected) =>
        Assert.Equal(expected, KanjiText.Replace(source, characterSet));

    [Fact]
    public void Replace_NoReplacement_ReturnsSameInstance()
    {
        var text = new string("高橋".ToCharArray());
        Assert.Same(text, KanjiText.Replace(text, JisX0208));
    }

    [Theory]
    [InlineData(TsujiIvs)]
    [InlineData(SakakiIvs)]
    public void Replace_RegisteredIvs_WithoutFallback_ReturnsSameInstance(string text) =>
        Assert.Same(text, KanjiText.Replace(text, JisX0208));

    [Theory]
    [InlineData("𠮷", true)]
    [InlineData("A", false)]
    [InlineData("高橋", false)]
    [InlineData("\u3404\U000E0101", true)]
    [InlineData("\u6B04\uFE00", true)]
    [InlineData("髙\uFE0F", false)]
    [InlineData(TsujiIvs, true)]
    [InlineData(SakakiIvs, true)]
    [InlineData("\u3406\U000E0100", true)]
    public void KanjiCharacter_TryParse_ReturnsExpected(string text, bool expected) =>
        Assert.Equal(expected, KanjiCharacter.TryParse(text, out _));

    [Theory]
    [InlineData("\u7CA4\U000E0103")]
    [InlineData("\u805A\U000E0104")]
    [InlineData("\U00020509\U000E0103")]
    [InlineData("\U00023AA3\U000E0100")]
    [InlineData("\U00023AA3\U000E0101")]
    [InlineData("\U00026BE7\U000E0100")]
    [InlineData("\U00026BE7\U000E0101")]
    public void KanjiCharacter_2026IvdAddition_IsRegistered(string text) =>
        Assert.True(KanjiCharacter.TryParse(text, out _));

    [Fact]
    public void TsujiIvs_OneDotShinnyo_HasExpectedCodePoints()
    {
        // フォントで字形が見えなくても、文字列中のVS符号位置を確認します。
        Assert.Equal("辻\U000E0100", TsujiIvs);
        Assert.True(KanjiCharacter.TryParse(TsujiIvs, out var parsed));
        Assert.Equal(0x8FBB, parsed.BaseCharacter.Value);
        Assert.Equal(0xE0100, parsed.VariationSelector?.Value);
    }

    [Fact]
    public void GetAlternatives_TsujiIvs_FallbackAddsBaseForStringAndTypedInput()
    {
        Assert.Empty(Kanji.GetAlternatives(TsujiIvs, JisX0208));
        Assert.Equal(new[] { "辻" }, Kanji.GetAlternatives(TsujiIvs, JisX0208, AllowVs).Select(x => x.ToString()));
        var parsed = KanjiCharacter.Parse(TsujiIvs);
        Assert.Equal("辻", Assert.Single(Kanji.GetAlternatives(parsed, JisX0208, AllowVs)).ToString());
    }

    [Fact]
    public void TryGetAlternative_TsujiIvs_FallbackRequiresOptionAndSupportedBase()
    {
        Assert.False(Kanji.TryGetAlternative(TsujiIvs, JisX0208, out _));
        Assert.True(Kanji.TryGetAlternative(TsujiIvs, JisX0208, out var alternative, AllowVs));
        Assert.Equal("辻", alternative.ToString());
        Assert.True(Kanji.TryGetAlternative(KanjiCharacter.Parse(TsujiIvs), JisX0208, out alternative, AllowVs));
        Assert.Equal("辻", alternative.ToString());
        Assert.False(Kanji.TryGetAlternative(TsujiIvs, Plane2, out _, AllowVs));
    }

    [Theory]
    [InlineData(JisX0208)]
    [InlineData(Plane1)]
    [InlineData(Plane2)]
    [InlineData(BothPlanes)]
    public void IsSupported_TsujiIvs_ReturnsFalseForEveryCharacterSet(CharacterSet characterSet) =>
        Assert.False(Kanji.IsSupported(TsujiIvs, characterSet));

    [Theory]
    [InlineData(JisX0208)]
    [InlineData(BothPlanes)]
    public void TextIsSupported_RegisteredIvs_ReturnsFalse(CharacterSet characterSet) =>
        Assert.False(KanjiText.IsSupported(TsujiIvs, characterSet));

    [Theory]
    [InlineData(JisX0208, "辻")]
    [InlineData(Plane1, "辻")]
    [InlineData(Plane2, TsujiIvs)]
    public void Replace_TsujiIvs_WithFallback_UsesSupportedBase(CharacterSet characterSet, string expected) =>
        Assert.Equal(expected, KanjiText.Replace(TsujiIvs, characterSet, AllowVs));

    [Fact]
    public void SakakiIvs_FallbackRequiresOption()
    {
        Assert.Equal("榊\U000E0100", SakakiIvs);
        Assert.True(KanjiCharacter.TryParse(SakakiIvs, out _));
        Assert.Empty(Kanji.GetAlternatives(SakakiIvs, JisX0208));
        Assert.Equal("榊", Assert.Single(Kanji.GetAlternatives(SakakiIvs, JisX0208, AllowVs)).ToString());
        Assert.False(Kanji.TryGetAlternative(SakakiIvs, JisX0208, out _));
        Assert.True(Kanji.TryGetAlternative(SakakiIvs, JisX0208, out var alternative, AllowVs));
        Assert.Equal("榊", alternative.ToString());
        Assert.Equal("榊", KanjiText.Replace(SakakiIvs, JisX0208, AllowVs));
    }

    [Fact]
    public void Svs_FallbackRequiresOptionAndSequenceRemainsUnsupported()
    {
        const string svs = "不\uFE00";
        Assert.True(KanjiCharacter.TryParse(svs, out _));
        Assert.Empty(Kanji.GetAlternatives(svs, JisX0208));
        Assert.Equal("不", Assert.Single(Kanji.GetAlternatives(svs, JisX0208, AllowVs)).ToString());
        Assert.True(Kanji.TryGetAlternative(svs, JisX0208, out var alternative, AllowVs));
        Assert.Equal("不", alternative.ToString());
        Assert.Equal("不", KanjiText.Replace(svs, JisX0208, AllowVs));
        Assert.Equal("不", KanjiText.Replace(svs, Plane1, AllowVs));
        Assert.False(Kanji.IsSupported(svs, Plane1));
        Assert.False(Kanji.IsSupported(svs, BothPlanes));
    }

    [Fact]
    public void Plane2Ivs_WithFallback_ReturnsBaseAndSequenceRemainsUnsupported()
    {
        const string ivs = "\u3406\U000E0100";
        Assert.True(KanjiCharacter.TryParse(ivs, out _));
        Assert.False(Kanji.IsSupported(ivs, Plane2));
        Assert.Equal("\u3406", Assert.Single(Kanji.GetAlternatives(ivs, Plane2, AllowVs)).ToString());
        Assert.True(Kanji.TryGetAlternative(ivs, Plane2, out var alternative, AllowVs));
        Assert.Equal("\u3406", alternative.ToString());
        Assert.Equal("\u3406", KanjiText.Replace(ivs, Plane2, AllowVs));
    }

    [Fact]
    public void VariationSelectorFallback_StopsAtBaseWithoutAnotherMjMapping()
    {
        const string svs = "\u59EC\uFE00";
        Assert.True(Kanji.TryGetAlternative(svs, Plane1, out var alternative, AllowVs));
        Assert.Equal("\u59EC", alternative.ToString());
        Assert.Equal("\u59EC", KanjiText.Replace(svs, Plane1, AllowVs));
    }

    [Fact]
    public void MjUniqueMapping_TakesPriorityOverVariationSelectorFallback()
    {
        const string ivs = "亟\U000E0102";
        Assert.True(Kanji.IsSupported("亟", JisX0208));
        Assert.True(Kanji.TryGetAlternative(ivs, JisX0208, out var alternative, AllowVs));
        Assert.Equal("丞", alternative.ToString());
        Assert.Equal("丞", KanjiText.Replace(ivs, JisX0208, AllowVs));
        var candidates = Kanji.GetAlternatives(ivs, JisX0208, AllowVs).Select(x => x.ToString()).ToArray();
        Assert.Contains("亟", candidates);
        Assert.Contains("丞", candidates);
        Assert.Equal(candidates.Length, candidates.Distinct().Count());
    }

    [Fact]
    public void GetAlternatives_MjCandidateEqualToFallbackBase_ReturnsItOnce()
    {
        var candidates = Kanji.GetAlternatives("辻\U000E0102", JisX0208, AllowVs).Select(x => x.ToString());
        Assert.Equal(1, candidates.Count(x => x == "辻"));
    }

    [Theory]
    [InlineData("邉　邉󠄏　邉󠄐　邉󠄑　邉󠄒　邉󠄓　邉󠄔　邉󠄕　邉󠄖　邉󠄗　邉󠄘　邉󠄙　邉󠄚　邉󠄛　邉󠄜　邉󠄝", "邉", 0xE010F)]
    [InlineData("邊　邊󠄈　邊󠄉　邊󠄊　邊󠄋　邊󠄌　邊󠄍　邊󠄎　邊󠄏　邊󠄐", "邊", 0xE0108)]
    public void RegisteredWatanabeIvs_MjMappingReplacesEachSequence(
        string row, string baseCharacter, int firstSelector)
    {
        // 各行の先頭はVSなし、以降は連続したVS付き表現です。
        var examples = row.Split('　');
        Assert.Equal(baseCharacter, examples[0]);
        Assert.True(Kanji.IsSupported(baseCharacter, JisX0208));
        for (int i = 1; i < examples.Length; i++)
        {
            int selector = firstSelector + i - 1;
            string example = examples[i];
            Assert.True(KanjiCharacter.TryParse(example, out var parsed));
            Assert.Equal(char.ConvertToUtf32(baseCharacter, 0), parsed.BaseCharacter.Value);
            Assert.Equal(selector, parsed.VariationSelector?.Value);
            Assert.False(Kanji.IsSupported(example, JisX0208));
            Assert.True(Kanji.TryGetAlternative(example, JisX0208, out var alternative));
            Assert.Equal(baseCharacter, alternative.ToString());
            Assert.Equal(baseCharacter, KanjiText.Replace(example, JisX0208));
            Assert.Equal(baseCharacter, KanjiText.Replace(example, JisX0208, AllowVs));
        }
    }

    [Fact]
    public void OutsideSetBase_WithFallback_DoesNotAddCandidateOrReplaceSequence()
    {
        const string ivs = "\u3404\U000E0101";
        Assert.Equal(Kanji.GetAlternatives(ivs, JisX0208).Select(x => x.ToString()),
            Kanji.GetAlternatives(ivs, JisX0208, AllowVs).Select(x => x.ToString()));
        Assert.False(Kanji.TryGetAlternative(ivs, JisX0208, out _, AllowVs));
        Assert.Equal(ivs, KanjiText.Replace(ivs, JisX0208, AllowVs));
    }

    [Fact]
    public void UnregisteredVariationSequence_WithFallback_IsPreservedAndScanContinues()
    {
        Assert.Equal("髙\uFE0F", KanjiText.Replace("髙\uFE0F", JisX0208));
        Assert.Equal("髙\uFE0F吉", KanjiText.Replace("髙\uFE0F𠮷", JisX0208, AllowVs));
        Assert.Equal("高\uFE0F", KanjiText.Replace("高\uFE0F", BothPlanes, AllowVs));
    }

    [Fact]
    public void InvalidUtf16_IsUnsupportedAndReplacePreservesItWhileContinuing()
    {
        Assert.False(KanjiText.IsSupported("高\ud800", JisX0208));
        Assert.Equal("高\ud800吉", KanjiText.Replace("髙\ud800𠮷", JisX0208));
    }

    [Fact]
    public void Replace_NullInput_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => KanjiText.Replace(null!, JisX0208));

    [Fact]
    public void Replace_SupplementaryTargetAndBmpTarget_WritesBothUtf16Lengths()
    {
        // 生成済みの一意変換 U+34F8 → U+20807 を使い、二符号単位の変換先も確認します。
        Assert.True(Kanji.TryGetAlternative("\u34F8", BothPlanes, out var target));
        Assert.Equal("\U00020807", target.ToString());
        Assert.Equal("\U00020807辻\U00020807",
            KanjiText.Replace("\u34F8辻\U000E0100\u34F8", BothPlanes, AllowVs));
    }
}
