// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal enum CliCommand { Check, Joyo }

internal sealed record CliOptions(string File, KanjiGrade? Grade, bool Unique, bool Json, CliCommand Command)
{
    internal const string Usage = "Usage: kanjivariants check <file.txt|file.srt> --grade <1-6> [--unique] [--json]\n"
        + "       kanjivariants joyo <file.txt|file.srt> [--unique] [--json]";

    internal static CliOptions Parse(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Expected command 'check' or 'joyo'.");
        var command = args[0] switch
        {
            "check" => CliCommand.Check,
            "joyo" => CliCommand.Joyo,
            _ => throw new ArgumentException("Expected command 'check' or 'joyo'."),
        };
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
                    if (command == CliCommand.Joyo)
                        throw new ArgumentException("--grade is not supported by 'joyo'.");
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
        if (command == CliCommand.Check && grade is null)
            throw new ArgumentException("--grade is required.");
        string extension = Path.GetExtension(args[1]);
        if (!string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".srt", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only .txt and .srt files are supported.");
        return new CliOptions(args[1], grade, unique, json, command);
    }
}
