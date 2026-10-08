// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text.Json;

namespace KanjiVariants.Cli;

internal static class JoyoCheckOutput
{
    private sealed class UniqueItem
    {
        internal JoyoKanjiDiagnosticItem First { get; }
        internal int Count { get; set; } = 1;
        internal UniqueItem(JoyoKanjiDiagnosticItem first) => First = first;
    }

    internal static void Write(JoyoTextCheck check, bool unique, bool json, TextWriter output)
    {
        var result = check.Diagnosis;
        var groups = unique ? Group(result.NonJoyoItems) : null;
        if (json)
        {
            // 教育漢字CLIと同様、集約時は最初の位置と件数だけを返します。
            object nonJoyo = groups is null
                ? result.NonJoyoItems.Select(item => new
                {
                    text = item.Original, reason = item.Reason.ToString(),
                    utf16Index = item.Utf16Index, utf16Length = item.Utf16Length,
                }).ToArray()
                : groups.Select(group => new
                {
                    text = group.First.Original, reason = group.First.Reason.ToString(),
                    firstUtf16Index = group.First.Utf16Index, count = group.Count,
                }).ToArray();
            output.WriteLine(JsonSerializer.Serialize(new
            {
                containsOnlyJoyoKanji = result.ContainsOnlyJoyoKanji,
                hasNonJoyoKanji = result.HasNonJoyoKanji,
                hasInvalidUtf16 = result.HasInvalidUtf16,
                nonJoyo,
            }, new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        output.WriteLine($"Contains only Joyo kanji: {(result.ContainsOnlyJoyoKanji ? "Yes" : "No")}");
        output.WriteLine($"Contains non-Joyo kanji: {(result.HasNonJoyoKanji ? "Yes" : "No")}");
        if (result.HasInvalidUtf16)
            output.WriteLine("Contains invalid UTF-16: Yes");
        output.WriteLine();
        if (result.NonJoyoItems.Count == 0)
            output.WriteLine("No non-Joyo kanji found.");
        else
        {
            output.WriteLine("Non-Joyo kanji:");
            if (groups is null)
                foreach (var item in result.NonJoyoItems)
                    output.WriteLine($"{item.Original}  {item.Reason}  index={item.Utf16Index}");
            else
                foreach (var group in groups)
                    output.WriteLine($"{group.First.Original}  {group.First.Reason}  count={group.Count}");
        }
    }

    private static List<UniqueItem> Group(IReadOnlyList<JoyoKanjiDiagnosticItem> items)
    {
        var groups = new List<UniqueItem>();
        var index = new Dictionary<(string Original, JoyoKanjiDiagnosticReason Reason), UniqueItem>();
        foreach (var item in items)
        {
            var key = (item.Original, item.Reason);
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
}
