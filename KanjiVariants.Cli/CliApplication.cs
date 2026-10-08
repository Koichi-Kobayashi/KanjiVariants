// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants.Cli;

internal static class CliApplication
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        CliOptions options;
        try
        {
            options = CliOptions.Parse(args);
        }
        catch (ArgumentException ex)
        {
            error.WriteLine($"Error: {ex.Message}");
            error.WriteLine(CliOptions.Usage);
            return 2;
        }

        CliInput input;
        try
        {
            input = CliInput.Read(options.File);
        }
        catch (FormatException ex)
        {
            error.WriteLine($"Error: {ex.Message}");
            return 2;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // DecoderFallbackExceptionもArgumentExceptionとして読込エラーに含めます。
            error.WriteLine(ex is DecoderFallbackException ? "Error: Input is not valid UTF-8."
                : $"Error: Could not read file: {ex.Message}");
            return 2;
        }

        if (input.Subtitles is not null)
        {
            var subtitles = SrtCheck.Analyze(input.Subtitles, options.Command, options.Grade);
            SrtCheckOutput.Write(subtitles, options.Unique, options.Json, output);
            return subtitles.ExitCode;
        }

        if (options.Command == CliCommand.Joyo)
        {
            var joyo = JoyoTextCheck.Analyze(input.Text);
            JoyoCheckOutput.Write(joyo, options.Unique, options.Json, output);
            return joyo.ExitCode;
        }

        var check = TextCheck.Analyze(input.Text, options.Grade!.Value);
        CheckOutput.Write(check, options.Unique, options.Json, output);
        return check.ExitCode;
    }
}
