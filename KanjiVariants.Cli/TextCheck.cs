// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal sealed class TextCheck
{
    internal EducationKanjiTextDiagnosticResult Diagnosis { get; }
    internal bool HasInvalidUtf16 { get; }
    internal int ExitCode => HasInvalidUtf16 ? 2 : Diagnosis.UnlearnedItems.Count == 0 ? 0 : 1;

    private TextCheck(EducationKanjiTextDiagnosticResult diagnosis, bool hasInvalidUtf16)
    {
        Diagnosis = diagnosis;
        HasInvalidUtf16 = hasInvalidUtf16;
    }

    internal static TextCheck Analyze(string text, KanjiGrade grade)
    {
        var diagnosis = EducationKanjiDiagnostics.AnalyzeText(text, grade);
        bool invalid = false;
        foreach (var item in diagnosis.Items)
            invalid |= item.Reason == EducationKanjiDiagnosticReason.InvalidUtf16;
        // 入力異常は、未習漢字の有無より優先して終了コードへ反映します。
        return new TextCheck(diagnosis, invalid);
    }
}
