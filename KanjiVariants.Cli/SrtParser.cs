// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Globalization;
using System.Text.RegularExpressions;

namespace KanjiVariants.Cli;

internal static class SrtParser
{
    private sealed record Line(int Start, int ContentEnd, string Content);
    private static readonly Regex Timing = new(
        @"^(?<start>[0-9]{2}:[0-5][0-9]:[0-5][0-9],[0-9]{3})[ \t]*-->[ \t]*(?<end>[0-9]{2}:[0-5][0-9]:[0-5][0-9],[0-9]{3})$",
        RegexOptions.CultureInvariant);

    internal static IReadOnlyList<SrtSubtitle> Parse(string text)
    {
        var lines = ReadLines(text);
        var subtitles = new List<SrtSubtitle>();
        var numbers = new HashSet<int>();
        int offset = 0;
        while (offset < lines.Count)
        {
            if (lines[offset].Content.Length == 0) { offset++; continue; }
            int numberLine = offset;
            if (!TryNumber(lines[offset++].Content, out int number))
                throw Error(numberLine, "Expected an integer subtitle number.");
            if (!numbers.Add(number))
                throw Error(numberLine, "Duplicate subtitle number.");
            if (offset >= lines.Count)
                throw Error(numberLine, "Missing timecode line.");
            var timing = Timing.Match(lines[offset].Content.Trim());
            if (!timing.Success)
                throw Error(offset, "Expected HH:MM:SS,mmm --> HH:MM:SS,mmm.");
            offset++;
            int firstLine = offset;
            while (offset < lines.Count && lines[offset].Content.Length != 0)
            {
                // 空行なしで次の字幕が始まる場合、番号・時刻を本文として診断しません。
                if (TryNumber(lines[offset].Content, out _) && offset + 1 < lines.Count &&
                    Timing.IsMatch(lines[offset + 1].Content.Trim()))
                    throw Error(offset, "Missing blank line between subtitle blocks.");
                offset++;
            }
            if (offset == firstLine)
                throw Error(firstLine, "Missing subtitle text.");
            int start = lines[firstLine].Start;
            string body = text.Substring(start, lines[offset - 1].ContentEnd - start);
            var textLines = new List<SrtTextLine>();
            for (int i = firstLine; i < offset; i++)
                textLines.Add(new SrtTextLine(lines[i].Start - start, i + 1));
            subtitles.Add(new SrtSubtitle(number, timing.Groups["start"].Value, timing.Groups["end"].Value,
                body, textLines.AsReadOnly()));
        }
        if (subtitles.Count == 0)
            throw Error(0, "No subtitle blocks found.");
        return subtitles.AsReadOnly();
    }

    private static bool TryNumber(string text, out int number) =>
        int.TryParse(text.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);

    private static FormatException Error(int line, string message) =>
        new($"Invalid SRT at source line {line + 1}: {message}");

    private static List<Line> ReadLines(string text)
    {
        var lines = new List<Line>();
        int offset = 0;
        while (offset < text.Length)
        {
            int start = offset;
            while (offset < text.Length && text[offset] is not ('\r' or '\n')) offset++;
            int contentEnd = offset;
            if (offset < text.Length)
            {
                if (text[offset++] == '\r' && offset < text.Length && text[offset] == '\n') offset++;
            }
            lines.Add(new Line(start, contentEnd, text.Substring(start, contentEnd - start)));
        }
        return lines;
    }
}
