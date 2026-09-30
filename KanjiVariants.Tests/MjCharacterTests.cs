// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class MjCharacterTests
{
    [Fact]
    public void GetByMjGlyphName_ExistingName_ReturnsOriginalFields()
    {
        var entry = Assert.IsType<MjCharacterEntry>(MjCharacter.GetByMjGlyphName("MJ030183"));
        Assert.Equal("MJ030183", entry.MjGlyphName);
        Assert.Equal(new Rune(0x6B04), entry.CorrespondingUcs);
        Assert.Equal(new Rune(0xF91D), entry.ImplementedUcs);
        Assert.Equal(new Rune(0xF91D), entry.CompatibilityIdeograph);
        Assert.Equal("1-86-27", entry.JisX0213);
        Assert.Equal(MjKanjiPolicy.Jinmeiyo, entry.KanjiPolicy);
        Assert.Equal("\u6B04\uFE00", Assert.Single(entry.Svs).ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("MJ999999")]
    [InlineData("mj030183")]
    [InlineData(" MJ030183")]
    public void GetByMjGlyphName_NotExactName_ReturnsNull(string name) =>
        Assert.Null(MjCharacter.GetByMjGlyphName(name));

    [Fact]
    public void GetByMjGlyphName_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => MjCharacter.GetByMjGlyphName(null!));

    // 以下の識別子と各列の値はmji.00602.xlsxで確認した固定例です。
    [Theory]
    [InlineData("髙", "MJ028902", MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CorrespondingUcs)]
    [InlineData("\u3404", "MJ000007", MjCharacterMatchKind.CorrespondingUcs)]
    [InlineData("\uF91D", "MJ030183", MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CompatibilityIdeograph)]
    [InlineData("\u6B04\uFE00", "MJ030183", MjCharacterMatchKind.Svs)]
    [InlineData("\u8FBB\U000E0102", "MJ025760", MjCharacterMatchKind.Ivs)]
    [InlineData("々", "MJ000001", MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CorrespondingUcs)]
    public void Find_OriginalRepresentation_ReturnsOneMatchWithCombinedReasons(
        string text, string name, MjCharacterMatchKind expected)
    {
        var match = Assert.Single(MjCharacter.Find(text), m => m.Entry.MjGlyphName == name);
        Assert.Equal(expected, match.MatchKind);
        Assert.Same(MjCharacter.GetByMjGlyphName(name), match.Entry);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("高高")]
    [InlineData("a")]
    [InlineData("\uD800")]
    [InlineData("\uDC00")]
    [InlineData("\u8FBB\U000E01EF")]
    [InlineData("\u8FBB\U000E0102x")]
    public void Find_InvalidOrMissingRepresentation_ReturnsEmpty(string text) =>
        Assert.Empty(MjCharacter.Find(text));

    [Fact]
    public void Find_Null_ThrowsArgumentNullException() =>
        Assert.Throws<ArgumentNullException>(() => MjCharacter.Find((string)null!));

    [Fact]
    public void Find_RegisteredIvs_DoesNotIncludeBaseScalarMatches()
    {
        var parsed = KanjiCharacter.Parse("\u8FBB\U000E0102");
        var matches = MjCharacter.Find(parsed);
        Assert.NotEmpty(matches);
        Assert.All(matches, match => Assert.Equal(MjCharacterMatchKind.Ivs, match.MatchKind));
        Assert.Same(matches, MjCharacter.Find(parsed.ToString()));
        Assert.NotSame(matches, MjCharacter.Find("辻"));
    }

    [Fact]
    public void Find_RepeatedLookup_ReturnsSharedReadOnlyResults()
    {
        var results = MjCharacter.Find("髙");
        Assert.Same(results, MjCharacter.Find("髙"));
        var list = Assert.IsAssignableFrom<IList<MjCharacterMatch>>(results);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Clear());
    }

    [Theory]
    [InlineData("MJ059399", "\U0002B9E4\U000E0100", "\u535A\U000E010A")]
    [InlineData("MJ059400", "\U0002B9E4\U000E0101", "\u535A\U000E010B")]
    public void GetByMjGlyphName_MultipleIvs_PreservesOriginalOrderAndIndexesEverySequence(
        string name, string first, string second)
    {
        var entry = Assert.IsType<MjCharacterEntry>(MjCharacter.GetByMjGlyphName(name));
        Assert.Equal(new[] { first, second }, entry.Ivs.Select(sequence => sequence.ToString()));
        foreach (var sequence in entry.Ivs)
        {
            var match = Assert.Single(MjCharacter.Find(sequence), match => match.Entry.MjGlyphName == name);
            Assert.Equal(MjCharacterMatchKind.Ivs, match.MatchKind);
            Assert.Same(entry, match.Entry);
        }
        var list = Assert.IsAssignableFrom<IList<KanjiCharacter>>(entry.Ivs);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Clear());
    }

    [Fact]
    public void GetByMjGlyphName_BlankSequences_UsesSharedEmptyLists()
    {
        var first = Assert.IsType<MjCharacterEntry>(MjCharacter.GetByMjGlyphName("MJ000001"));
        var second = Assert.IsType<MjCharacterEntry>(MjCharacter.GetByMjGlyphName("MJ028902"));
        Assert.Empty(first.Ivs);
        Assert.Empty(first.Svs);
        Assert.Same(first.Ivs, first.Svs);
        Assert.Same(first.Ivs, second.Ivs);
        Assert.Same(first.Svs, second.Svs);
        Assert.Null(first.KanjiPolicy);
    }

    [Fact]
    public void GetByMjGlyphName_Svs_ReturnsReadOnlyListWithoutBaseFallback()
    {
        var entry = Assert.IsType<MjCharacterEntry>(MjCharacter.GetByMjGlyphName("MJ030183"));
        var list = Assert.IsAssignableFrom<IList<KanjiCharacter>>(entry.Svs);
        Assert.True(list.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => list.Clear());
        var matches = MjCharacter.Find(Assert.Single(entry.Svs));
        Assert.All(matches, match => Assert.Equal(MjCharacterMatchKind.Svs, match.MatchKind));
        Assert.NotSame(matches, MjCharacter.Find("欄"));
    }

    [Fact]
    public void GetByMjGlyphName_AllOriginalEntries_AreUniqueAndPolicyConsistent()
    {
        int total = 0, corresponding = 0, implemented = 0, compatibility = 0, jis = 0, joyo = 0, jinmeiyo = 0;
        int ivsRows = 0, svsRows = 0, ivsCount = 0, svsCount = 0;
        var joyoScalars = new HashSet<int>();
        var jinmeiyoScalars = new HashSet<int>();
        // 原典で確認した最大識別子までを走査し、欠番を許容して全58,862行を確認します。
        for (int id = 1; id <= 68101; id++)
        {
            string name = $"MJ{id:D6}";
            var entry = MjCharacter.GetByMjGlyphName(name);
            if (entry is null) continue;
            total++;
            Assert.Equal(name, entry.MjGlyphName);
            CheckScalar(entry.CorrespondingUcs, MjCharacterMatchKind.CorrespondingUcs, ref corresponding);
            CheckScalar(entry.ImplementedUcs, MjCharacterMatchKind.ImplementedUcs, ref implemented);
            CheckScalar(entry.CompatibilityIdeograph, MjCharacterMatchKind.CompatibilityIdeograph, ref compatibility);
            if (entry.Ivs.Count > 0) ivsRows++;
            if (entry.Svs.Count > 0) svsRows++;
            CheckSequences(entry.Ivs, MjCharacterMatchKind.Ivs, ref ivsCount);
            CheckSequences(entry.Svs, MjCharacterMatchKind.Svs, ref svsCount);
            if (entry.JisX0213 is not null) jis++;
            if (entry.KanjiPolicy is MjKanjiPolicy policy)
            {
                Assert.NotNull(entry.ImplementedUcs);
                var scalar = entry.ImplementedUcs!.Value;
                switch (policy)
                {
                    case MjKanjiPolicy.Joyo:
                        joyo++;
                        Assert.True(JoyoKanji.IsJoyo(scalar.ToString()), name);
                        Assert.True(joyoScalars.Add(scalar.Value), name);
                        break;
                    case MjKanjiPolicy.Jinmeiyo:
                        jinmeiyo++;
                        Assert.True(JinmeiyoKanji.IsJinmeiyoKanji(scalar.ToString()), name);
                        Assert.True(jinmeiyoScalars.Add(scalar.Value), name);
                        break;
                    default:
                        Assert.Fail($"未知の漢字施策: {name}");
                        break;
                }
            }

            void CheckScalar(Rune? rune, MjCharacterMatchKind kind, ref int count)
            {
                if (rune is not Rune value) return;
                count++;
                Assert.True(Rune.IsValid(value.Value));
                var matches = MjCharacter.Find(value.ToString());
                Assert.Equal(matches.Count, matches.Select(match => match.Entry.MjGlyphName).Distinct().Count());
                var match = Assert.Single(matches, match => match.Entry.MjGlyphName == name);
                Assert.True(match.MatchKind.HasFlag(kind), name);
                Assert.Same(entry, match.Entry);
            }
            void CheckSequences(IReadOnlyList<KanjiCharacter> sequences, MjCharacterMatchKind kind, ref int count)
            {
                foreach (var sequence in sequences)
                {
                    count++;
                    Assert.True(sequence.HasVariationSelector, name);
                    Assert.True(KanjiCharacter.TryParse(sequence.ToString(), out var parsed), name);
                    Assert.Equal(sequence, parsed);
                    var matches = MjCharacter.Find(sequence);
                    Assert.Equal(matches.Count, matches.Select(match => match.Entry.MjGlyphName).Distinct().Count());
                    var match = Assert.Single(matches, match => match.Entry.MjGlyphName == name);
                    Assert.True(match.MatchKind.HasFlag(kind), name);
                    Assert.Same(entry, match.Entry);
                }
            }
        }
        Assert.Equal(58862, total);
        Assert.Equal(58859, corresponding);
        Assert.Equal(52607, implemented);
        Assert.Equal(101, compatibility);
        Assert.Equal(13707, jis);
        Assert.Equal(2136, joyo);
        Assert.Equal(863, jinmeiyo);
        Assert.Equal(11382, ivsRows);
        Assert.Equal(11384, ivsCount);
        Assert.Equal(89, svsRows);
        Assert.Equal(89, svsCount);
    }
}
