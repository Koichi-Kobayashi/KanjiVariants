// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal sealed record SrtDiagnosticItem(string Original, int Utf16Index, int Utf16Length,
    KanjiGrade? Grade, string Reason, SrtPosition Position);

internal sealed class SrtCheck
{
    private static readonly KanjiGrade[] Grades = { KanjiGrade.Grade1, KanjiGrade.Grade2,
        KanjiGrade.Grade3, KanjiGrade.Grade4, KanjiGrade.Grade5, KanjiGrade.Grade6 };
    internal CliCommand Command { get; }
    internal KanjiGrade? TargetGrade { get; }
    internal KanjiGrade? HighestEducationKanjiGrade { get; private set; }
    internal bool ContainsOnlyAllowedKanji { get; private set; } = true;
    internal bool HasNonTableKanji { get; private set; }
    internal bool HasInvalidUtf16 { get; private set; }
    internal IReadOnlyList<SrtDiagnosticItem> Items { get; private set; } = Array.Empty<SrtDiagnosticItem>();
    internal IReadOnlyList<SrtDiagnosticItem> Problems { get; private set; } = Array.Empty<SrtDiagnosticItem>();
    internal int ExitCode => HasInvalidUtf16 ? 2 : Problems.Count == 0 ? 0 : 1;

    private SrtCheck(CliCommand command, KanjiGrade? grade) { Command = command; TargetGrade = grade; }

    internal static SrtCheck Analyze(IReadOnlyList<SrtSubtitle> subtitles, CliCommand command, KanjiGrade? grade)
    {
        var check = new SrtCheck(command, grade);
        var items = new List<SrtDiagnosticItem>();
        var problems = new List<SrtDiagnosticItem>();
        foreach (var subtitle in subtitles)
        {
            if (command == CliCommand.Check)
            {
                var result = EducationKanjiDiagnostics.AnalyzeText(subtitle.Text, grade!.Value);
                check.ContainsOnlyAllowedKanji &= result.ContainsOnlyLearnedKanji;
                check.HasNonTableKanji |= result.HasNonEducationKanji;
                if (GradeNumber(result.HighestEducationKanjiGrade) > (GradeNumber(check.HighestEducationKanjiGrade) ?? 0))
                    check.HighestEducationKanjiGrade = result.HighestEducationKanjiGrade;
                foreach (var item in result.Items)
                {
                    var mapped = new SrtDiagnosticItem(item.Original, item.Utf16Index, item.Utf16Length,
                        item.Grade, item.Reason.ToString(), subtitle.Locate(item.Utf16Index));
                    items.Add(mapped);
                    check.HasInvalidUtf16 |= item.Reason == EducationKanjiDiagnosticReason.InvalidUtf16;
                    if (item.Reason is EducationKanjiDiagnosticReason.LearnedInLaterGrade or EducationKanjiDiagnosticReason.NotEducationKanji)
                        problems.Add(mapped);
                }
            }
            else
            {
                var result = JoyoKanjiDiagnostics.AnalyzeText(subtitle.Text);
                check.ContainsOnlyAllowedKanji &= result.ContainsOnlyJoyoKanji;
                check.HasNonTableKanji |= result.HasNonJoyoKanji;
                check.HasInvalidUtf16 |= result.HasInvalidUtf16;
                foreach (var item in result.Items)
                {
                    var mapped = new SrtDiagnosticItem(item.Original, item.Utf16Index, item.Utf16Length,
                        null, item.Reason.ToString(), subtitle.Locate(item.Utf16Index));
                    items.Add(mapped);
                    if (item.Reason == JoyoKanjiDiagnosticReason.NotJoyoKanji)
                        problems.Add(mapped);
                }
            }
        }
        check.Items = items.AsReadOnly();
        check.Problems = problems.AsReadOnly();
        return check;
    }

    // 集計・JSON学年表記はenum値の大小に依存させません。
    internal static int? GradeNumber(KanjiGrade? grade) => grade is null ? null : Array.IndexOf(Grades, grade.Value) + 1;
}
