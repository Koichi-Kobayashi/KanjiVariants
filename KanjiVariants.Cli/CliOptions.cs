// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal sealed record CliOptions(string File, KanjiGrade Grade, bool Unique, bool Json)
{
    internal const string Usage = "Usage: kanjivariants check <file.txt> --grade <1-6> [--unique] [--json]";

    internal static CliOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] != "check")
            throw new ArgumentException("Expected command 'check'.");
        if (args.Length < 2 || args[1].StartsWith("--", StringComparison.Ordinal))
            throw new ArgumentException("A text file is required.");

        KanjiGrade? grade = null;
        bool unique = false;
        bool json = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 2; i < args.Length; i++)
        {
            string option = args[i];
            if (!seen.Add(option))
                throw new ArgumentException($"Duplicate option: {option}");
            switch (option)
            {
                case "--grade":
                    if (++i >= args.Length)
                        throw new ArgumentException("--grade requires a value from 1 to 6.");
                    // 数値のキャストに依存せず、CLIの学年表記を既存enumへ対応付けます。
                    grade = args[i] switch
                    {
                        "1" => KanjiGrade.Grade1,
                        "2" => KanjiGrade.Grade2,
                        "3" => KanjiGrade.Grade3,
                        "4" => KanjiGrade.Grade4,
                        "5" => KanjiGrade.Grade5,
                        "6" => KanjiGrade.Grade6,
                        _ => throw new ArgumentException("--grade must be a value from 1 to 6."),
                    };
                    break;
                case "--unique": unique = true; break;
                case "--json": json = true; break;
                default: throw new ArgumentException($"Unexpected argument: {option}");
            }
        }
        if (grade is null)
            throw new ArgumentException("--grade is required.");
        if (!string.Equals(Path.GetExtension(args[1]), ".txt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .txt files are supported.");
        return new CliOptions(args[1], grade.Value, unique, json);
    }
}
