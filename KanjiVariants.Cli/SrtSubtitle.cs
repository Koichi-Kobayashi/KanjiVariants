// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal sealed record SrtTextLine(int Utf16Index, int SourceLineNumber);
internal sealed record SrtPosition(int SubtitleNumber, string StartTime, string EndTime,
    int TextLine, int Utf16IndexInLine, int SourceLineNumber);

internal sealed record SrtSubtitle(int SubtitleNumber, string StartTime, string EndTime,
    string Text, IReadOnlyList<SrtTextLine> TextLines)
{
    internal SrtPosition Locate(int utf16Index)
    {
        if (utf16Index < 0 || utf16Index >= Text.Length)
            throw new ArgumentOutOfRangeException(nameof(utf16Index));
        int line = 0;
        while (line + 1 < TextLines.Count && TextLines[line + 1].Utf16Index <= utf16Index)
            line++;
        return new SrtPosition(SubtitleNumber, StartTime, EndTime, line + 1,
            utf16Index - TextLines[line].Utf16Index, TextLines[line].SourceLineNumber);
    }
}
