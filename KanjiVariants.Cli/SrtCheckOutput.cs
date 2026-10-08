// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text.Json;

namespace KanjiVariants.Cli;

internal static class SrtCheckOutput
{
    private sealed class Group
    {
        internal SrtDiagnosticItem First { get; }
        internal int Count { get; set; } = 1;
        internal Group(SrtDiagnosticItem first) => First = first;
    }

    internal static void Write(SrtCheck check, bool unique, bool json, TextWriter output)
    {
        bool education = check.Command == CliCommand.Check;
        var groups = Collect(check.Problems, unique);
        if (json)
        {
            var root = new Dictionary<string, object?>();
            if (education)
            {
                root.Add("targetGrade", SrtCheck.GradeNumber(check.TargetGrade));
                root.Add("highestEducationKanjiGrade", SrtCheck.GradeNumber(check.HighestEducationKanjiGrade));
                root.Add("hasNonEducationKanji", check.HasNonTableKanji);
                root.Add("containsOnlyLearnedKanji", check.ContainsOnlyAllowedKanji);
            }
            else
            {
                root.Add("containsOnlyJoyoKanji", check.ContainsOnlyAllowedKanji);
                root.Add("hasNonJoyoKanji", check.HasNonTableKanji);
            }
            root.Add("hasInvalidUtf16", check.HasInvalidUtf16);
            root.Add(education ? "unlearned" : "nonJoyo", groups.Select(group => JsonItem(group, education, unique)).ToArray());
            output.WriteLine(JsonSerializer.Serialize(root, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        if (education)
        {
            output.WriteLine($"Target grade: {check.TargetGrade}");
            output.WriteLine($"Highest education-kanji grade: {check.HighestEducationKanjiGrade?.ToString() ?? "-"}");
            output.WriteLine($"Contains non-education kanji: {(check.HasNonTableKanji ? "Yes" : "No")}");
        }
        else
        {
            output.WriteLine($"Contains only Joyo kanji: {(check.ContainsOnlyAllowedKanji ? "Yes" : "No")}");
            output.WriteLine($"Contains non-Joyo kanji: {(check.HasNonTableKanji ? "Yes" : "No")}");
        }
        if (check.HasInvalidUtf16) output.WriteLine("Contains invalid UTF-16: Yes");
        output.WriteLine();
        if (groups.Count == 0)
            output.WriteLine(education ? "No unlearned kanji found." : "No non-Joyo kanji found.");
        else
        {
            output.WriteLine(education ? "Unlearned kanji:" : "Non-Joyo kanji:");
            foreach (var group in groups)
            {
                var item = group.First;
                var position = item.Position;
                string label = item.Grade?.ToString() ?? item.Reason;
                string location = unique
                    ? $"count={group.Count}  firstSubtitle={position.SubtitleNumber}  firstTime={position.StartTime}-{position.EndTime}  firstLine={position.TextLine}  firstIndex={position.Utf16IndexInLine}"
                    : $"subtitle={position.SubtitleNumber}  time={position.StartTime}-{position.EndTime}  line={position.TextLine}  index={position.Utf16IndexInLine}";
                output.WriteLine($"{item.Original}  {label}  {location}");
            }
        }
    }

    private static Dictionary<string, object?> JsonItem(Group group, bool education, bool unique)
    {
        var item = group.First;
        var position = item.Position;
        var row = new Dictionary<string, object?> { ["text"] = item.Original };
        if (education) row.Add("grade", SrtCheck.GradeNumber(item.Grade));
        row.Add("reason", item.Reason);
        if (unique)
        {
            row.Add("firstUtf16Index", item.Utf16Index);
            row.Add("count", group.Count);
            row.Add("firstSubtitleNumber", position.SubtitleNumber);
            row.Add("firstStartTime", position.StartTime);
            row.Add("firstEndTime", position.EndTime);
            row.Add("firstTextLine", position.TextLine);
            row.Add("firstUtf16IndexInLine", position.Utf16IndexInLine);
        }
        else
        {
            row.Add("utf16Index", item.Utf16Index);
            row.Add("utf16Length", item.Utf16Length);
            row.Add("subtitleNumber", position.SubtitleNumber);
            row.Add("startTime", position.StartTime);
            row.Add("endTime", position.EndTime);
            row.Add("textLine", position.TextLine);
            row.Add("utf16IndexInLine", position.Utf16IndexInLine);
        }
        return row;
    }

    private static List<Group> Collect(IReadOnlyList<SrtDiagnosticItem> items, bool unique)
    {
        var groups = new List<Group>();
        var index = new Dictionary<(string Original, KanjiGrade? Grade, string Reason), Group>();
        foreach (var item in items)
        {
            var key = (item.Original, item.Grade, item.Reason);
            if (unique && index.TryGetValue(key, out var group)) group.Count++;
            else
            {
                group = new Group(item);
                groups.Add(group);
                if (unique) index.Add(key, group);
            }
        }
        return groups;
    }
}
