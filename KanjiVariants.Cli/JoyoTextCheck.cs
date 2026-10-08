// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal sealed class JoyoTextCheck
{
    internal JoyoKanjiDiagnosticResult Diagnosis { get; }
    // APIの診断結果を利用し、CLIで文字判定を重複実装しません。入力異常を最優先します。
    internal int ExitCode => Diagnosis.HasInvalidUtf16 ? 2 : Diagnosis.HasNonJoyoKanji ? 1 : 0;

    private JoyoTextCheck(JoyoKanjiDiagnosticResult diagnosis) => Diagnosis = diagnosis;

    internal static JoyoTextCheck Analyze(string text) => new(JoyoKanjiDiagnostics.AnalyzeText(text));
}
