// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using KanjiVariants.Cli;
using Xunit;

namespace KanjiVariants.Cli.Tests;

public sealed class JoyoCliTests
{
    [Theory]
    [InlineData("", false, false)]
    [InlineData("--unique", true, false)]
    [InlineData("--json", false, true)]
    [InlineData("--json --unique", true, true)]
    public void Parse_JoyoOptions_RequiresNoGrade(string flags, bool unique, bool json)
    {
        var args = new[] { "joyo", "sample.txt" }.Concat(flags.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
        var options = CliOptions.Parse(args);
        Assert.Equal(CliCommand.Joyo, options.Command);
        Assert.Null(options.Grade);
        Assert.Equal(unique, options.Unique);
        Assert.Equal(json, options.Json);
    }

    [Theory]
    [InlineData("joyo")]
    [InlineData("joyo --json")]
    [InlineData("joyo sample.txt extra.txt")]
    [InlineData("joyo sample.csv")]
    [InlineData("joyo sample.txt --other")]
    [InlineData("joyo sample.txt --unique --unique")]
    [InlineData("joyo sample.txt --json --json")]
    [InlineData("joyo sample.txt --grade 3")]
    [InlineData("joyo sample.txt --grade")]
    [InlineData("joyo sample.txt 3")]
    public void Run_InvalidJoyoArguments_Returns2AndWritesOnlyToStderr(string command)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        Assert.Equal(2, CliApplication.Run(command.Split(' '), output, error));
        Assert.Equal("", output.ToString());
        Assert.Contains(CliOptions.Usage, error.ToString());
    }

    [Fact]
    public void Run_JoyoMissingFile_Returns2WithoutJsonOutput()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        Assert.Equal(2, CliApplication.Run(new[] { "joyo", path, "--json" }, output, error));
        Assert.Equal("", output.ToString());
        Assert.Contains("Could not read file", error.ToString());
    }

    [Theory]
    [InlineData("高会議", 0)]
    [InlineData("あいうABC123々〆〻😀", 0)]
    [InlineData("", 0)]
    [InlineData("明日は髙橋さんと會議です。", 1)]
    [InlineData("\uF900𠮷", 1)]
    [InlineData("不\uFE00学\U000E01EF", 1)]
    public void Run_JoyoText_ReturnsApiSummaryAndExpectedExitCode(string text, int exitCode)
    {
        WithFile(Encoding.UTF8.GetBytes(text), file =>
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(exitCode, CliApplication.Run(new[] { "joyo", file }, output, error));
            Assert.Equal("", error.ToString());
            Assert.Contains($"Contains only Joyo kanji: {(exitCode == 0 ? "Yes" : "No")}", output.ToString());
            Assert.Contains($"Contains non-Joyo kanji: {(exitCode == 0 ? "No" : "Yes")}", output.ToString());
            Assert.Contains(exitCode == 0 ? "No non-Joyo kanji found." : "Non-Joyo kanji:", output.ToString());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_JoyoPlainOutput_UniquePreservesFirstOccurrenceOrder(bool unique)
    {
        WithFile(Encoding.UTF8.GetBytes("高髙會髙"), file =>
        {
            var args = new List<string> { "joyo", file };
            if (unique) args.Add("--unique");
            var output = new StringWriter();
            Assert.Equal(1, CliApplication.Run(args.ToArray(), output, new StringWriter()));
            string expected = unique
                ? "髙  NotJoyoKanji  count=2" + Environment.NewLine + "會  NotJoyoKanji  count=1"
                : "髙  NotJoyoKanji  index=1" + Environment.NewLine + "會  NotJoyoKanji  index=2"
                    + Environment.NewLine + "髙  NotJoyoKanji  index=3";
            Assert.Contains(expected, output.ToString());
            Assert.DoesNotContain(unique ? "index=" : "count=", output.ToString());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_JoyoJson_IsJsonOnlyWithCamelCaseSchemaAndUtf16Positions(bool unique)
    {
        const string text = "高髙會髙𠮷不\uFE00学\U000E01EF\uF900";
        WithFile(Encoding.UTF8.GetBytes(text), file =>
        {
            var args = new List<string> { "joyo", file, "--json" };
            if (unique) args.Add("--unique");
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(1, CliApplication.Run(args.ToArray(), output, error));
            Assert.Equal("", error.ToString());
            using var json = JsonDocument.Parse(output.ToString());
            var root = json.RootElement;
            Assert.Equal(new[] { "containsOnlyJoyoKanji", "hasNonJoyoKanji", "hasInvalidUtf16", "nonJoyo" },
                root.EnumerateObject().Select(x => x.Name));
            Assert.False(root.GetProperty("containsOnlyJoyoKanji").GetBoolean());
            Assert.True(root.GetProperty("hasNonJoyoKanji").GetBoolean());
            Assert.False(root.GetProperty("hasInvalidUtf16").GetBoolean());
            var expected = JoyoKanjiDiagnostics.AnalyzeText(text).NonJoyoItems;
            var rows = root.GetProperty("nonJoyo").EnumerateArray().ToArray();
            var groups = expected.GroupBy(x => (x.Original, x.Reason)).ToArray();
            Assert.Equal(unique ? groups.Length : expected.Count, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                var first = unique ? groups[i].First() : expected[i];
                Assert.Equal(first.Original, rows[i].GetProperty("text").GetString());
                Assert.Equal(first.Reason.ToString(), rows[i].GetProperty("reason").GetString());
                Assert.Equal(first.Utf16Index, rows[i].GetProperty(unique ? "firstUtf16Index" : "utf16Index").GetInt32());
                Assert.Equal(unique ? groups[i].Count() : first.Utf16Length,
                    rows[i].GetProperty(unique ? "count" : "utf16Length").GetInt32());
                Assert.Equal(unique ? new[] { "text", "reason", "firstUtf16Index", "count" }
                    : new[] { "text", "reason", "utf16Index", "utf16Length" }, rows[i].EnumerateObject().Select(x => x.Name));
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_JoyoUtf8BomOrNoBom_PreservesPositions(bool bom)
    {
        byte[] bytes = Encoding.UTF8.GetBytes("高髙");
        if (bom) bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(bytes).ToArray();
        WithFile(bytes, file =>
        {
            var output = new StringWriter();
            Assert.Equal(1, CliApplication.Run(new[] { "joyo", file }, output, new StringWriter()));
            Assert.Contains("髙  NotJoyoKanji  index=1", output.ToString());
        });
    }

    [Fact]
    public void Run_JoyoInvalidUtf8_Returns2WithStderrOnly()
    {
        WithFile(new byte[] { 0xED, 0xA0, 0x80 }, file =>
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(2, CliApplication.Run(new[] { "joyo", file, "--json" }, output, error));
            Assert.Equal("", output.ToString());
            Assert.Contains("Input is not valid UTF-8", error.ToString());
        });
    }

    [Theory]
    [InlineData("\r\n", 3)]
    [InlineData("\n", 2)]
    public void Run_JoyoLineEndings_ArePreserved(string newline, int index)
    {
        WithFile(Encoding.UTF8.GetBytes("高" + newline + "髙"), file =>
        {
            var output = new StringWriter();
            Assert.Equal(1, CliApplication.Run(new[] { "joyo", file }, output, new StringWriter()));
            Assert.Contains($"髙  NotJoyoKanji  index={index}", output.ToString());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Analyze_JoyoInvalidUtf16_Returns2BeforeNonJoyoAndIsVisibleInBothOutputs(bool nonJoyo)
    {
        var check = JoyoTextCheck.Analyze(new string('\uD800', 1) + (nonJoyo ? "髙" : ""));
        Assert.Equal(2, check.ExitCode);
        Assert.Equal(nonJoyo, check.Diagnosis.HasNonJoyoKanji);
        var output = new StringWriter();
        JoyoCheckOutput.Write(check, false, true, output);
        using var json = JsonDocument.Parse(output.ToString());
        Assert.True(json.RootElement.GetProperty("hasInvalidUtf16").GetBoolean());
        Assert.False(json.RootElement.GetProperty("containsOnlyJoyoKanji").GetBoolean());
        Assert.Equal(nonJoyo ? 1 : 0, json.RootElement.GetProperty("nonJoyo").GetArrayLength());
        var plain = new StringWriter();
        JoyoCheckOutput.Write(check, false, false, plain);
        Assert.Contains("Contains invalid UTF-16: Yes", plain.ToString());
        if (!nonJoyo) Assert.Contains("No non-Joyo kanji found.", plain.ToString());
    }

    [Fact]
    public void Run_JoyoOnlyJoyoJson_EmitsEmptyNonJoyoArray()
    {
        WithFile(Encoding.UTF8.GetBytes("高会議"), file =>
        {
            var output = new StringWriter();
            Assert.Equal(0, CliApplication.Run(new[] { "joyo", file, "--json", "--unique" }, output, new StringWriter()));
            using var json = JsonDocument.Parse(output.ToString());
            Assert.True(json.RootElement.GetProperty("containsOnlyJoyoKanji").GetBoolean());
            Assert.False(json.RootElement.GetProperty("hasNonJoyoKanji").GetBoolean());
            Assert.Empty(json.RootElement.GetProperty("nonJoyo").EnumerateArray());
        });
    }

    private static void WithFile(byte[] bytes, Action<string> test)
    {
        string file = Path.Combine(Path.GetTempPath(), "kanjivariants joyo " + Guid.NewGuid() + ".txt");
        try
        {
            File.WriteAllBytes(file, bytes);
            test(file);
        }
        finally { File.Delete(file); }
    }
}
