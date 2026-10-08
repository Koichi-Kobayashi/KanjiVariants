// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;
using Xunit;

namespace KanjiVariants.Tests;

public sealed class EducationKanjiDiagnosticsTests
{
    private static readonly KanjiGrade[] Grades = { KanjiGrade.Grade1, KanjiGrade.Grade2,
        KanjiGrade.Grade3, KanjiGrade.Grade4, KanjiGrade.Grade5, KanjiGrade.Grade6 };

    [Theory]
    [InlineData("", 0, false, false, 0)]
    [InlineData("学校へ行く", 0, false, false, 0)]
    [InlineData("議", 0, false, false, 1)]
    [InlineData("催", 0, false, true, 1)]
    [InlineData("", 0xD800, true, false, 0)]
    [InlineData("", 0xDC00, true, false, 0)]
    [InlineData("議", 0xD800, true, false, 1)]
    [InlineData("催", 0xDC00, true, true, 1)]
    public void AnalyzeText_InvalidUtf16Summary_IsIndependentOfUnlearnedAndNonEducationKanji(
        string text, int invalidCodeUnit, bool hasInvalidUtf16, bool hasNonEducationKanji, int unlearnedCount)
    {
        // テストランナーによる文字列の直列化で不正UTF-16が置換されないよう、実行時に構築します。
        if (invalidCodeUnit != 0) text = ((char)invalidCodeUnit).ToString() + text;
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade3);
        Assert.Equal(hasInvalidUtf16, result.HasInvalidUtf16);
        Assert.Equal(hasNonEducationKanji, result.HasNonEducationKanji);
        Assert.Equal(unlearnedCount, result.UnlearnedItems.Count);
        Assert.DoesNotContain(result.UnlearnedItems,
            item => item.Reason == EducationKanjiDiagnosticReason.InvalidUtf16);
        if (hasInvalidUtf16) Assert.False(result.ContainsOnlyLearnedKanji);
        AssertConsistent(text, KanjiGrade.Grade3);
    }

    [Theory]
    [InlineData("", null, false)]
    [InlineData("あいうえお", null, false)]
    [InlineData("日学校", KanjiGrade.Grade1, false)]
    [InlineData("学高漢茨囲", KanjiGrade.Grade5, false)]
    [InlineData("囲茨漢高学", KanjiGrade.Grade5, false)]
    [InlineData("日胃", KanjiGrade.Grade6, false)]
    [InlineData("学校催", KanjiGrade.Grade1, true)]
    [InlineData("催議日", KanjiGrade.Grade4, true)]
    [InlineData("催髙𠮷", null, true)]
    [InlineData("学\U000E0100", null, true)]
    [InlineData("不\uFE00", null, true)]
    [InlineData("学\U000E0100日", KanjiGrade.Grade1, true)]
    public void AnalyzeText_GradeSummary_ReturnsHighestEducationGradeRegardlessOfTargetGrade(
        string text, KanjiGrade? highestEducationKanjiGrade, bool hasNonEducationKanji)
    {
        foreach (var targetGrade in Grades)
        {
            var result = EducationKanjiDiagnostics.AnalyzeText(text, targetGrade);
            Assert.Equal(highestEducationKanjiGrade, result.HighestEducationKanjiGrade);
            Assert.Equal(hasNonEducationKanji, result.HasNonEducationKanji);
            AssertConsistent(text, targetGrade);
        }
    }

    [Fact]
    public void AnalyzeText_InvalidUtf16Only_DoesNotContributeToGradeSummary()
    {
        string text = new(new[] { '\uD800', '\uD800' });
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade6);
        Assert.All(result.Items, item => Assert.Equal(EducationKanjiDiagnosticReason.InvalidUtf16, item.Reason));
        Assert.Null(result.HighestEducationKanjiGrade);
        Assert.False(result.HasNonEducationKanji);
        Assert.False(result.ContainsOnlyLearnedKanji);
        AssertConsistent(text, KanjiGrade.Grade6);
    }

    [Fact]
    public void AnalyzeText_All1026EducationKanji_HighestEducationKanjiGradeIsGrade6WithoutNonEducationKanji()
    {
        string text = string.Concat(Grades.SelectMany(EducationKanji.GetByGrade).Select(x => x.ToString()));
        foreach (var targetGrade in Grades)
        {
            var result = EducationKanjiDiagnostics.AnalyzeText(text, targetGrade);
            Assert.Equal(1026, result.Items.Count);
            Assert.Equal(KanjiGrade.Grade6, result.HighestEducationKanjiGrade);
            Assert.False(result.HasNonEducationKanji);
            AssertConsistent(text, targetGrade);
        }
    }

    [Theory]
    [InlineData("学", KanjiGrade.Grade1)]
    [InlineData("高", KanjiGrade.Grade2)]
    [InlineData("漢", KanjiGrade.Grade3)]
    [InlineData("茨", KanjiGrade.Grade4)]
    [InlineData("囲", KanjiGrade.Grade5)]
    [InlineData("胃", KanjiGrade.Grade6)]
    public void AnalyzeText_GradeBoundary_ExactGradeIsLearnedAndPreviousGradeIsNot(string text, KanjiGrade grade)
    {
        var exact = Assert.Single(EducationKanjiDiagnostics.AnalyzeText(text, grade).Items);
        Assert.Equal(grade, exact.Grade);
        Assert.True(exact.IsLearned);
        Assert.Equal(EducationKanjiDiagnosticReason.Learned, exact.Reason);
        int index = Array.IndexOf(Grades, grade);
        if (index > 0)
        {
            var later = Assert.Single(EducationKanjiDiagnostics.AnalyzeText(text, Grades[index - 1]).Items);
            Assert.Equal(grade, later.Grade);
            Assert.False(later.IsLearned);
            Assert.Equal(EducationKanjiDiagnosticReason.LearnedInLaterGrade, later.Reason);
        }
    }

    [Theory]
    [InlineData("催")]
    [InlineData("髙")]
    [InlineData("學")]
    [InlineData("\uF900")]
    [InlineData("𠮷")]
    [InlineData("\U0002F800")]
    [InlineData("学\U000E0100")]
    [InlineData("不\uFE00")]
    [InlineData("学\U000E01EF")]
    [InlineData("𠮷\U000E0100")]
    public void AnalyzeText_OutsideTableOrVariationSequence_DoesNotConvertToBase(string text)
    {
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade6);
        var item = Assert.Single(result.Items);
        Assert.Equal(text, item.Original);
        Assert.Null(item.Grade);
        Assert.False(item.IsLearned);
        Assert.False(result.ContainsOnlyLearnedKanji);
        Assert.Equal(EducationKanjiDiagnosticReason.NotEducationKanji, item.Reason);
        Assert.Same(item, Assert.Single(result.UnlearnedItems));
        AssertConsistent(text, KanjiGrade.Grade6);
    }

    [Theory]
    [InlineData("々")]
    [InlineData("〆")]
    [InlineData("〻")]
    [InlineData("あ")]
    [InlineData("ア")]
    [InlineData("A")]
    [InlineData("0")]
    [InlineData("。")]
    [InlineData("😀")]
    [InlineData("A\uFE00")]
    [InlineData("😀\uFE0F")]
    [InlineData("\uFE00")]
    [InlineData("\U000E0100")]
    [InlineData("\0")]
    public void AnalyzeText_NonKanji_IsOutsideScopeAndLearned(string text)
    {
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade1);
        var item = Assert.Single(result.Items);
        Assert.Equal(EducationKanjiDiagnosticReason.NotKanji, item.Reason);
        Assert.Null(item.Grade);
        Assert.True(item.IsLearned);
        Assert.True(result.ContainsOnlyLearnedKanji);
        Assert.Empty(result.UnlearnedItems);
        AssertConsistent(text, KanjiGrade.Grade1);
    }

    [Fact]
    public void AnalyzeText_SampleSentence_ReturnsActualGradesAndUnlearnedKanjiInOrder()
    {
        var result = EducationKanjiDiagnostics.AnalyzeText("明日は会議を開催します。", KanjiGrade.Grade3);
        Assert.False(result.ContainsOnlyLearnedKanji);
        Assert.Equal(new KanjiGrade?[] { KanjiGrade.Grade2, KanjiGrade.Grade1, null,
            KanjiGrade.Grade2, KanjiGrade.Grade4, null, KanjiGrade.Grade3, null, null, null, null, null },
            result.Items.Select(x => x.Grade));
        Assert.Equal(new[] { "議", "催" }, result.UnlearnedItems.Select(x => x.Original));
        Assert.Equal(EducationKanjiDiagnosticReason.LearnedInLaterGrade, result.UnlearnedItems[0].Reason);
        Assert.Equal(EducationKanjiDiagnosticReason.NotEducationKanji, result.UnlearnedItems[1].Reason);
        Assert.True(JoyoKanji.IsJoyo("催"));
        AssertConsistent(result.Text, result.TargetGrade);
    }

    [Fact]
    public void AnalyzeText_JinmeiyoFixture_ReturnsNotEducationKanji()
    {
        // 髙などの異体字とは区別し、既存の人名用漢字データで確認済みの丑を使います。
        Assert.True(JinmeiyoKanji.IsJinmeiyoKanji("丑"));
        var result = EducationKanjiDiagnostics.AnalyzeText("丑", KanjiGrade.Grade6);
        var item = Assert.Single(result.Items);
        Assert.Equal(EducationKanjiDiagnosticReason.NotEducationKanji, item.Reason);
        Assert.False(item.IsLearned);
        Assert.Null(item.Grade);
        Assert.Same(item, Assert.Single(result.UnlearnedItems));
        AssertConsistent("丑", KanjiGrade.Grade6);
    }

    [Fact]
    public void AnalyzeText_Positions_CoverSupplementaryIvsInvalidUtf16AndSeparateCrLf()
    {
        string broken = new('\uD800', 1);
        string text = "A𠮷学\U000E0100" + broken + "日\r\n";
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade1);
        Assert.Equal(new[] { 0, 1, 3, 6, 7, 8, 9 }, result.Items.Select(x => x.Utf16Index));
        Assert.Equal(new[] { 1, 2, 3, 1, 1, 1, 1 }, result.Items.Select(x => x.Utf16Length));
        Assert.Equal(EducationKanjiDiagnosticReason.InvalidUtf16, result.Items[3].Reason);
        Assert.Equal(broken, result.Items[3].Original);
        Assert.Equal(4, Assert.Single(EducationKanjiDiagnostics.AnalyzeText("𠮷\U000E0100", KanjiGrade.Grade6).Items).Utf16Length);
        AssertConsistent(text, KanjiGrade.Grade1);
    }

    [Theory]
    [InlineData('\uD800')]
    [InlineData('\uDC00')]
    [InlineData('\uDB40')]
    public void AnalyzeText_IsolatedSurrogate_IsInvalidButExcludedFromUnlearnedItems(char value)
    {
        string broken = new(value, 1);
        foreach (string text in new[] { broken, "日" + broken, broken + "日", "日" + broken + "A" })
        {
            var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade1);
            var invalid = Assert.Single(result.Items, x => x.Reason == EducationKanjiDiagnosticReason.InvalidUtf16);
            Assert.Equal(1, invalid.Utf16Length);
            Assert.False(invalid.IsLearned);
            Assert.Null(invalid.Grade);
            Assert.False(result.ContainsOnlyLearnedKanji);
            Assert.Empty(result.UnlearnedItems);
            AssertConsistent(text, KanjiGrade.Grade1);
        }
    }

    [Fact]
    public void AnalyzeText_MalformedPairsAndConsecutiveSelectors_PreserveReaderBoundaries()
    {
        string text = new string(new[] { '\uD800', '\uD800', '\uDC00', '日', '\uDC00' });
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade1);
        Assert.Equal(new[] { 1, 2, 1, 1 }, result.Items.Select(x => x.Utf16Length));
        Assert.Equal(2, result.Items.Count(x => x.Reason == EducationKanjiDiagnosticReason.InvalidUtf16));
        AssertConsistent(text, KanjiGrade.Grade1);
        string selectors = "学\U000E0100\uFE00日";
        Assert.Equal(new[] { "学\U000E0100", "\uFE00", "日" },
            EducationKanjiDiagnostics.AnalyzeText(selectors, KanjiGrade.Grade1).Items.Select(x => x.Original));
        AssertConsistent(selectors, KanjiGrade.Grade1);
    }

    [Fact]
    public void AnalyzeText_Empty_PreservesInputAndGradeWithSharedEmptyLists()
    {
        var result = EducationKanjiDiagnostics.AnalyzeText("", KanjiGrade.Grade6);
        Assert.Same("", result.Text);
        Assert.Equal(KanjiGrade.Grade6, result.TargetGrade);
        Assert.True(result.ContainsOnlyLearnedKanji);
        Assert.Empty(result.Items);
        Assert.Empty(result.UnlearnedItems);
        Assert.Same(result.Items, result.UnlearnedItems);
        Assert.Null(result.HighestEducationKanjiGrade);
        Assert.False(result.HasNonEducationKanji);
    }

    [Fact]
    public void AnalyzeText_Collections_AreReadOnlyAndShareUnlearnedItemInstances()
    {
        string text = new("日胃髙胃".ToCharArray());
        var result = EducationKanjiDiagnostics.AnalyzeText(text, KanjiGrade.Grade1);
        Assert.Same(text, result.Text);
        Assert.Equal(new[] { "胃", "髙", "胃" }, result.UnlearnedItems.Select(x => x.Original));
        for (int i = 0; i < result.UnlearnedItems.Count; i++)
            Assert.Same(result.Items[i + 1], result.UnlearnedItems[i]);
        Assert.Throws<NotSupportedException>(() => ((IList<EducationKanjiTextDiagnosticItem>)result.Items).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<EducationKanjiTextDiagnosticItem>)result.UnlearnedItems).Clear());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(99)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void AnalyzeText_InvalidGrade_ThrowsWithTargetGradeParamNameEvenForEmptyText(int grade) =>
        Assert.Throws<ArgumentOutOfRangeException>("targetGrade", () => EducationKanjiDiagnostics.AnalyzeText("", (KanjiGrade)grade));

    [Fact]
    public void AnalyzeText_Null_ThrowsBeforeValidatingGrade() =>
        Assert.Throws<ArgumentNullException>("text", () => EducationKanjiDiagnostics.AnalyzeText(null!, (KanjiGrade)99));

    [Fact]
    public void AnalyzeText_All1026CharactersForAllTargetGrades_MatchEducationKanjiAndCumulativeMembership()
    {
        // 全配当文字を各対象学年で照合し、学年の境界と文章全体の不変条件を固定します。
        for (int sourceOrder = 0; sourceOrder < Grades.Length; sourceOrder++)
        {
            var characters = EducationKanji.GetByGrade(Grades[sourceOrder]);
            string text = string.Concat(characters.Select(x => x.ToString()));
            for (int targetOrder = 0; targetOrder < Grades.Length; targetOrder++)
            {
                var result = EducationKanjiDiagnostics.AnalyzeText(text, Grades[targetOrder]);
                Assert.Equal(characters.Count, result.Items.Count);
                Assert.Equal(Grades[sourceOrder], result.HighestEducationKanjiGrade);
                Assert.False(result.HasNonEducationKanji);
                Assert.All(result.Items, item =>
                {
                    Assert.Equal(Grades[sourceOrder], item.Grade);
                    Assert.Equal(sourceOrder <= targetOrder ? EducationKanjiDiagnosticReason.Learned
                        : EducationKanjiDiagnosticReason.LearnedInLaterGrade, item.Reason);
                });
                AssertConsistent(text, Grades[targetOrder]);
            }
        }
    }

    private static void AssertConsistent(string text, KanjiGrade targetGrade)
    {
        var result = EducationKanjiDiagnostics.AnalyzeText(text, targetGrade);
        Assert.Same(text, result.Text);
        Assert.Equal(targetGrade, result.TargetGrade);
        Assert.Equal(text, string.Concat(result.Items.Select(x => x.Original)));
        Assert.Equal(result.Items.All(x => x.IsLearned), result.ContainsOnlyLearnedKanji);
        Assert.Equal(result.Items.Any(x => x.Reason == EducationKanjiDiagnosticReason.InvalidUtf16),
            result.HasInvalidUtf16);
        var unlearned = result.Items.Where(x => x.Reason is EducationKanjiDiagnosticReason.LearnedInLaterGrade
            or EducationKanjiDiagnosticReason.NotEducationKanji).ToArray();
        Assert.Equal(unlearned, result.UnlearnedItems);
        Assert.Equal(result.Items.Any(x => x.Reason == EducationKanjiDiagnosticReason.NotEducationKanji),
            result.HasNonEducationKanji);
        // enum値ではなく明示した学年順を使い、全項目から求めた最高学年と集計値を照合します。
        KanjiGrade? highestEducationKanjiGrade = null;
        foreach (var grade in Grades)
            if (result.Items.Any(x => x.Grade == grade))
                highestEducationKanjiGrade = grade;
        Assert.Equal(highestEducationKanjiGrade, result.HighestEducationKanjiGrade);
        int offset = 0;
        foreach (var item in result.Items)
        {
            Assert.Equal(offset, item.Utf16Index);
            Assert.InRange(item.Utf16Length, 1, 4);
            Assert.Equal(item.Original.Length, item.Utf16Length);
            Assert.Equal(text.Substring(offset, item.Utf16Length), item.Original);
            Assert.Equal(item.Reason is EducationKanjiDiagnosticReason.NotKanji or EducationKanjiDiagnosticReason.Learned, item.IsLearned);
            if (item.Reason is EducationKanjiDiagnosticReason.Learned or EducationKanjiDiagnosticReason.LearnedInLaterGrade)
            {
                Assert.Equal(EducationKanji.GetGrade(item.Original), item.Grade);
                int gradeOrder = Array.IndexOf(Grades, item.Grade!.Value);
                Assert.Equal(gradeOrder <= Array.IndexOf(Grades, targetGrade), item.IsLearned);
            }
            else
                Assert.Null(item.Grade);
            offset += item.Utf16Length;
        }
        Assert.Equal(text.Length, offset);
    }
}
