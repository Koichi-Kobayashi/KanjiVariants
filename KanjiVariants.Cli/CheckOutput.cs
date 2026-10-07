// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text.Json;

namespace KanjiVariants.Cli;

internal static class CheckOutput
{
    private sealed record UniqueKey(string Original, KanjiGrade? Grade, EducationKanjiDiagnosticReason Reason);
    private sealed class UniqueItem
    {
        internal EducationKanjiTextDiagnosticItem First { get; }
        internal int Count { get; set; } = 1;
        internal UniqueItem(EducationKanjiTextDiagnosticItem first) => First = first;
    }

    internal static void Write(TextCheck check, bool unique, bool json, TextWriter output)
    {
        var result = check.Diagnosis;
        var groups = unique ? Group(result.UnlearnedItems) : null;
        if (json)
        {
            // 通常出力と集約出力の位置情報を別スキーマにして、最初の位置であることを明示します。
            object unlearned = groups is null
                ? result.UnlearnedItems.Select(item => new
                {
                    text = item.Original, grade = GradeNumber(item.Grade), reason = item.Reason.ToString(),
                    utf16Index = item.Utf16Index, utf16Length = item.Utf16Length,
                }).ToArray()
                : groups.Select(group => new
                {
                    text = group.First.Original, grade = GradeNumber(group.First.Grade), reason = group.First.Reason.ToString(),
                    firstUtf16Index = group.First.Utf16Index, count = group.Count,
                }).ToArray();
            output.WriteLine(JsonSerializer.Serialize(new
            {
                targetGrade = GradeNumber(result.TargetGrade),
                highestEducationKanjiGrade = GradeNumber(result.HighestEducationKanjiGrade),
                hasNonEducationKanji = result.HasNonEducationKanji,
                containsOnlyLearnedKanji = result.ContainsOnlyLearnedKanji,
                hasInvalidUtf16 = check.HasInvalidUtf16,
                unlearned,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        output.WriteLine($"Target grade: {result.TargetGrade}");
        output.WriteLine($"Highest education-kanji grade: {result.HighestEducationKanjiGrade?.ToString() ?? "-"}");
        output.WriteLine($"Contains non-education kanji: {(result.HasNonEducationKanji ? "Yes" : "No")}");
        if (check.HasInvalidUtf16)
            output.WriteLine("Invalid UTF-16 detected in input.");
        output.WriteLine();
        if (result.UnlearnedItems.Count == 0)
            output.WriteLine("No unlearned kanji found.");
        else
        {
            output.WriteLine("Unlearned kanji:");
            if (groups is null)
                foreach (var item in result.UnlearnedItems)
                    output.WriteLine($"{item.Original}  {Label(item)}  index={item.Utf16Index}");
            else
                foreach (var group in groups)
                    output.WriteLine($"{group.First.Original}  {Label(group.First)}  count={group.Count}");
        }
    }

    private static string Label(EducationKanjiTextDiagnosticItem item) => item.Grade?.ToString() ?? item.Reason.ToString();

    private static List<UniqueItem> Group(IReadOnlyList<EducationKanjiTextDiagnosticItem> items)
    {
        var groups = new List<UniqueItem>();
        var index = new Dictionary<UniqueKey, UniqueItem>();
        foreach (var item in items)
        {
            var key = new UniqueKey(item.Original, item.Grade, item.Reason);
            if (index.TryGetValue(key, out var group))
                group.Count++;
            else
            {
                group = new UniqueItem(item);
                index.Add(key, group);
                groups.Add(group);
            }
        }
        return groups;
    }

    private static int? GradeNumber(KanjiGrade? grade) => grade switch
    {
        null => null,
        KanjiGrade.Grade1 => 1,
        KanjiGrade.Grade2 => 2,
        KanjiGrade.Grade3 => 3,
        KanjiGrade.Grade4 => 4,
        KanjiGrade.Grade5 => 5,
        KanjiGrade.Grade6 => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(grade)),
    };
}
