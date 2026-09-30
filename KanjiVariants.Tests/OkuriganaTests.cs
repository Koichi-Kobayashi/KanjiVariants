// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class OkuriganaTests
{
    [Theory]
    [InlineData("書く", 1, OkuriganaRuleKind.Principle)]
    [InlineData("著しい", 1, OkuriganaRuleKind.Exception)]
    [InlineData("行う", 1, OkuriganaRuleKind.Permitted)]
    [InlineData("動かす", 2, OkuriganaRuleKind.Principle)]
    [InlineData("月", 3, OkuriganaRuleKind.Principle)]
    [InlineData("辺り", 3, OkuriganaRuleKind.Exception)]
    [InlineData("謡", 4, OkuriganaRuleKind.Exception)]
    [InlineData("必ず", 5, OkuriganaRuleKind.Principle)]
    [InlineData("又", 5, OkuriganaRuleKind.Exception)]
    [InlineData("長引く", 6, OkuriganaRuleKind.Principle)]
    [InlineData("取締役", 7, OkuriganaRuleKind.Exception)]
    public void Find_OfficialWord_ReturnsExpectedRuleAndKind(string word, int rule, OkuriganaRuleKind kind)
    {
        var entry = Assert.Single(Okurigana.Find(word));
        Assert.Equal(word, entry.Word);
        Assert.Equal(rule, entry.RuleNumber);
        Assert.Equal(kind, entry.Kind);
    }

    [Fact]
    public void Find_PermittedSpelling_ReturnsSameEntryAsPrimarySpelling()
    {
        var primary = Assert.Single(Okurigana.Find("行う"));
        Assert.Same(primary, Assert.Single(Okurigana.Find("行なう")));
        Assert.Equal(new[] { "行なう" }, primary.AlternativeForms);
        var compound = Assert.Single(Okurigana.Find("打合せる"));
        Assert.Equal("打ち合わせる", compound.Word);
        Assert.Equal(new[] { "打ち合せる", "打合せる" }, compound.AlternativeForms);
        Assert.Equal(OkuriganaRuleKind.Permitted, compound.Kind);
    }

    [Fact]
    public void Find_RelatedWordOrReading_IsNotIndexedAsAlternative()
    {
        var related = Assert.Single(Okurigana.Find("動かす"));
        Assert.Empty(related.AlternativeForms);
        Assert.Contains("〔動く〕", related.Note!);
        Assert.Empty(Okurigana.Find("動く"));
        Assert.Empty(Okurigana.Find("おどかす"));
        var readings = Okurigana.Find("脅かす");
        Assert.Equal(2, readings.Count);
        Assert.All(readings, entry => Assert.Empty(entry.AlternativeForms));
        Assert.Contains(readings, entry => entry.Note!.Contains("（おどかす）"));
        Assert.Contains(readings, entry => entry.Note!.Contains("（おびやかす）"));
    }

    [Fact]
    public void Find_Rule2PermittedSquareBrackets_AreAlternativeForms()
    {
        var entry = Assert.Single(Okurigana.Find("浮ぶ"));
        Assert.Equal("浮かぶ", entry.Word);
        Assert.Equal(2, entry.RuleNumber);
        Assert.Equal(OkuriganaRuleKind.Permitted, entry.Kind);
        Assert.Equal(new[] { "浮ぶ" }, entry.AlternativeForms);
    }

    [Fact]
    public void Find_Appendix_PreservesSectionAndPermittedSpellings()
    {
        var entry = Assert.Single(Okurigana.Find("差し支える"));
        Assert.Null(entry.RuleNumber);
        Assert.Equal(OkuriganaRuleKind.Appendix, entry.Kind);
        Assert.Equal(new[] { "差支える" }, entry.AlternativeForms);
        Assert.Same(entry, Assert.Single(Okurigana.Find("差支える")));
        var noOkurigana = Assert.Single(Okurigana.Find("息吹"));
        Assert.Equal(OkuriganaRuleKind.Appendix, noOkurigana.Kind);
        Assert.Contains("送り仮名を付けない", noOkurigana.Note!);
    }

    [Fact]
    public void Find_Rule7_MarkedWordPreservesOriginalNotationInNote()
    {
        var entry = Assert.Single(Okurigana.Find("博多織"));
        Assert.Equal(7, entry.RuleNumber);
        Assert.Contains("原典表記: 《博多》織", entry.Note!);
        Assert.Contains("他の漢字で置き換えた場合", entry.Note!);
        Assert.Empty(entry.AlternativeForms);
    }

    [Fact]
    public void Find_SameWordInDifferentSections_PreservesBothEntries()
    {
        var entries = Okurigana.Find("浮かぶ");
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, entry => entry.Kind == OkuriganaRuleKind.Principle);
        Assert.Contains(entries, entry => entry.Kind == OkuriganaRuleKind.Permitted);
    }

    [Fact]
    public void Find_NfcNormalizesWithoutFoldingKanaOrPartialMatching()
    {
        var entry = Assert.Single(Okurigana.Find("さび止め"));
        Assert.Same(entry, Assert.Single(Okurigana.Find("さひ\u3099止め")));
        Assert.Empty(Okurigana.Find("サビ止め"));
        Assert.Empty(Okurigana.Find("行"));
        Assert.Empty(Okurigana.Find("行う "));
    }

    [Theory]
    [InlineData("")]
    [InlineData("未掲載の語")]
    public void Find_NoMatch_ReturnsSharedEmptyList(string word)
    {
        Assert.Empty(Okurigana.Find(word));
        Assert.Same(Okurigana.Find(word), Okurigana.Find("未掲載の別の語"));
    }

    [Fact]
    public void Find_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => Okurigana.Find(null!));

    [Theory]
    [InlineData(1, 71)]
    [InlineData(2, 67)]
    [InlineData(3, 30)]
    [InlineData(4, 73)]
    [InlineData(5, 29)]
    [InlineData(6, 112)]
    [InlineData(7, 86)]
    public void GetByRule_ReturnsOnlySpecifiedRuleAndExpectedCount(int rule, int count)
    {
        var entries = Okurigana.GetByRule(rule);
        Assert.Equal(count, entries.Count);
        Assert.All(entries, entry => Assert.Equal(rule, entry.RuleNumber));
        Assert.Same(entries, Okurigana.GetByRule(rule));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(8)]
    public void GetByRule_OutOfRange_ThrowsArgumentOutOfRangeException(int rule) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Okurigana.GetByRule(rule));

    [Theory]
    [InlineData(OkuriganaRuleKind.Principle, 208)]
    [InlineData(OkuriganaRuleKind.Exception, 200)]
    [InlineData(OkuriganaRuleKind.Permitted, 60)]
    [InlineData(OkuriganaRuleKind.Appendix, 15)]
    public void GetByKind_ReturnsOnlySpecifiedKindAndExpectedCount(OkuriganaRuleKind kind, int count)
    {
        var entries = Okurigana.GetByKind(kind);
        Assert.Equal(count, entries.Count);
        Assert.All(entries, entry => Assert.Equal(kind, entry.Kind));
        Assert.Same(entries, Okurigana.GetByKind(kind));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void GetByKind_InvalidValue_ThrowsArgumentOutOfRangeException(int kind) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Okurigana.GetByKind((OkuriganaRuleKind)kind));

    [Fact]
    public void ReturnedCollections_AreSharedAndReadOnly()
    {
        var entry = Assert.Single(Okurigana.Find("行う"));
        Assert.Same(Okurigana.Find("行う"), Okurigana.Find("行う"));
        foreach (var values in new[] { Okurigana.Find("行う"), Okurigana.GetByRule(1), Okurigana.GetByKind(OkuriganaRuleKind.Permitted) })
            Assert.Throws<NotSupportedException>(() => ((IList<OkuriganaEntry>)values).Add(entry));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)entry.AlternativeForms).Add("別表記"));
    }

    [Fact]
    public void GeneratedCorpus_HasNoEmptyOrDuplicateEntriesAndConsistentIndexes()
    {
        var entries = Enum.GetValues<OkuriganaRuleKind>().SelectMany(Okurigana.GetByKind).ToArray();
        Assert.Equal(483, entries.Length);
        Assert.Equal(483, entries.Select(entry => (entry.Word, entry.RuleNumber, entry.Kind,
            Forms: string.Join("\n", entry.AlternativeForms), entry.Note)).Distinct().Count());
        Assert.Equal(63, entries.Count(entry => entry.AlternativeForms.Count > 0));
        Assert.Equal(72, entries.Sum(entry => entry.AlternativeForms.Count));
        Assert.All(entries, entry =>
        {
            Assert.NotEmpty(entry.Word);
            Assert.Equal(entry.Kind == OkuriganaRuleKind.Appendix, entry.RuleNumber is null);
            if (entry.RuleNumber is int rule)
            {
                Assert.InRange(rule, 1, 7);
                Assert.Contains(Okurigana.GetByRule(rule), value => ReferenceEquals(value, entry));
            }
            Assert.Contains(Okurigana.Find(entry.Word), value => ReferenceEquals(value, entry));
            Assert.All(entry.AlternativeForms, form =>
            {
                Assert.NotEmpty(form);
                Assert.Contains(Okurigana.Find(form), value => ReferenceEquals(value, entry));
            });
        });
    }
}
