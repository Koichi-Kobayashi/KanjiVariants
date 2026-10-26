// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class MjFallbackTests
{
    [Theory]
    [InlineData("高", "髙")]
    [InlineData("斎", "齋")]
    [InlineData("崎", "﨑")]
    [InlineData("辻", "辻\U000E0102")]
    [InlineData("辻", "辻\U000E0103")]
    [InlineData("欄", "欄\uFE00")]
    public void SourcesContainDocumentedDirectRelation(string target, string expected)
    {
        var sources = MjFallback.GetSources(target);
        var source = Assert.Single(sources, x => x.Character.ToString() == expected);
        Assert.NotEmpty(source.Evidence);
        Assert.All(source.Evidence, e =>
        {
            Assert.Equal(source.Character, e.Source);
            Assert.Equal(target, e.Target.ToString());
            Assert.Matches("^MJ[0-9]{6}$", e.MjGlyphName);
            Assert.NotEqual(MjCharacterMatchKind.None, e.SourceRepresentationKind);
            Assert.NotNull(e.JisX0213);
        });
        Assert.Same(sources, MjFallback.GetSources(KanjiCharacter.Parse(target)));
    }

    [Fact]
    public void MultipleReasonsKeepOriginalMetadata()
    {
        var high = Assert.Single(MjFallback.GetSources("高"), x => x.Character.ToString() == "髙");
        Assert.Equal(2, high.Evidence.Count);
        Assert.All(high.Evidence, e => Assert.Equal("MJ028902", e.MjGlyphName));
        Assert.Contains(high.Evidence, e => e.RelationKind == MjFallbackRelationKind.JisInclusionAndUcsUnification);
        var notification = Assert.Single(high.Evidence, e => e.RelationKind == MjFallbackRelationKind.FamilyRegisterNotification);
        Assert.Equal(1, notification.HopCount);
        Assert.Equal("戸籍統一文字情報 親字・正字", notification.NotificationType);
    }

    [Fact]
    public void EvidenceAcrossDifferentMjGlyphsIsNotLost()
    {
        var source = Assert.Single(MjFallback.GetSources("〆"), x => x.Character.ToString() == "乄");
        Assert.True(source.Evidence.Select(e => e.MjGlyphName).Distinct().Count() > 1);
        Assert.Contains(source.Evidence, e => e.MjGlyphName == "MJ006376");
    }

    [Fact]
    public void ResultsAreDistinctStableAndDoNotContainSelf()
    {
        foreach (var target in new[] { "高", "斎", "崎", "辻", "欄" })
        {
            var sources = MjFallback.GetSources(target);
            Assert.DoesNotContain(sources, x => x.Character.ToString() == target);
            Assert.Equal(sources.Count, sources.Select(x => x.Character).Distinct().Count());
            var expected = sources.OrderBy(x => x.Character.BaseCharacter.Value)
                .ThenBy(x => x.Character.VariationSelector?.Value ?? 0).Select(x => x.Character);
            Assert.Equal(expected, sources.Select(x => x.Character));
            Assert.All(sources, source => Assert.Equal(source.Evidence.Count, source.Evidence.Distinct().Count()));
        }
    }

    [Fact]
    public void SupplementarySourceIsOneScalarAndNotNormalized()
    {
        var supplementary = Assert.Single(MjFallback.GetSources("高"), x => x.Character.ToString() == "\U00020160");
        Assert.Equal(0x20160, supplementary.Character.BaseCharacter.Value);
        Assert.Equal(2, supplementary.Character.ToString().Length);
        Assert.Empty(MjFallback.GetSources("髙"));
        Assert.Empty(MjFallback.GetSources("高\U000E0100"));
        Assert.Empty(MjFallback.GetSources("\U000323AF"));
        Assert.Empty(MjFallback.GetSources(default(KanjiCharacter)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("高橋")]
    [InlineData("A")]
    [InlineData("髙\uFE0F")]
    [InlineData("\uFE00")]
    public void InvalidInputIsRejected(string input) => Assert.Throws<FormatException>(() => MjFallback.GetSources(input));

    [Fact]
    public void InvalidUtf16IsRejectedWithoutTestRunnerStringSerialization()
    {
        // 属性の文字列シリアライズで不正サロゲートが置き換わらないよう実行時に構築する。
        foreach (var input in new[] { new string('\uD800', 1), new string('\uDC00', 1), "高" + new string('\uD800', 1) })
            Assert.Throws<FormatException>(() => MjFallback.GetSources(input));
    }

    [Fact]
    public void NullIsRejected() => Assert.Throws<ArgumentNullException>(() => MjFallback.GetSources((string)null!));

    [Fact]
    public async Task ConcurrentCallsShareImmutableResult()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => MjFallback.GetSources("崎"))));
        Assert.All(results, r => Assert.Same(results[0], r));
        Assert.Throws<NotSupportedException>(() => ((IList<MjFallbackSource>)results[0]).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<MjFallbackEvidence>)results[0][0].Evidence).Clear());
    }

    [Fact]
    public void AllGeneratedSourcesRoundTripAndKeepOriginalDirection()
    {
        int candidates = 0, evidenceCount = 0;
        foreach (int cp in GeneratedMjFallbackData.Targets)
        {
            var target = KanjiCharacter.Parse(new System.Text.Rune(cp).ToString());
            var sources = MjFallback.GetSources(target);
            Assert.Equal(sources.Count, sources.Select(s => s.Character).Distinct().Count());
            foreach (var source in sources)
            {
                Assert.Equal(source.Character, KanjiCharacter.Parse(source.Character.ToString()));
                Assert.NotEqual(target, source.Character);
                Assert.NotEmpty(source.Evidence);
                Assert.All(source.Evidence, e =>
                {
                    Assert.Equal(target, e.Target);
                    Assert.Equal(source.Character, e.Source);
                    Assert.True(Enum.IsDefined(e.RelationKind));
                });
                candidates++;
                evidenceCount += source.Evidence.Count;
            }
        }
        Assert.Equal(39852, candidates);
        Assert.Equal(62481, evidenceCount);
    }

    [Fact]
    public void ExistingForwardApisKeepTheirMeaning()
    {
        Assert.Equal(new[] { "高" }, Kanji.GetAlternatives("高", CharacterSet.JisX0208).Select(c => c.ToString()));
        Assert.Equal(new[] { "高" }, Kanji.GetAlternatives("髙", CharacterSet.JisX0208).Select(c => c.ToString()));
        Assert.True(Kanji.TryGetAlternative("髙", CharacterSet.JisX0208, out var target));
        Assert.Equal("高", target.ToString());
        Assert.False(Kanji.IsSupported("髙", CharacterSet.JisX0208));
    }
}
