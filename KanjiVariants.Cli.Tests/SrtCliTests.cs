// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using KanjiVariants.Cli;
using Xunit;

namespace KanjiVariants.Cli.Tests;

public sealed class SrtCliTests
{
    private const string Timing = "00:01:15,200 --> 00:01:18,300";

    [Theory]
    [InlineData("\n", "")]
    [InlineData("\n", "\n\n")]
    [InlineData("\r\n", "")]
    [InlineData("\r\n", "\r\n\r\n")]
    public void Parse_MultilineWithOrWithoutTrailingBlank_PreservesTextAndTimes(string newline, string trailing)
    {
        var subtitle = Assert.Single(SrtParser.Parse("12" + newline + Timing + newline + "明日は" + newline + "会議です。" + trailing));
        Assert.Equal(12, subtitle.SubtitleNumber);
        Assert.Equal("00:01:15,200", subtitle.StartTime);
        Assert.Equal("00:01:18,300", subtitle.EndTime);
        Assert.Equal("明日は" + newline + "会議です。", subtitle.Text);
        var position = subtitle.Locate(4 + newline.Length);
        Assert.Equal(2, position.TextLine);
        Assert.Equal(1, position.Utf16IndexInLine);
        Assert.Equal(4, position.SourceLineNumber);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\t  ")]
    public void Parse_WhitespaceOnlyText_IsAccepted(string body) =>
        Assert.Equal(body, Assert.Single(SrtParser.Parse("1\n" + Timing + "\n" + body)).Text);

    [Fact]
    public void Parse_NonSequentialNumbersAndExtraSeparators_PreservesOrder()
    {
        var subtitles = SrtParser.Parse("\n1\n" + Timing + "\n高\n\n\n3\n" + Timing + "\n会\n\n5\n" + Timing + "\n議");
        Assert.Equal(new[] { 1, 3, 5 }, subtitles.Select(x => x.SubtitleNumber));
        Assert.Equal(new[] { "高", "会", "議" }, subtitles.Select(x => x.Text));
        Assert.Throws<NotSupportedException>(() => ((IList<SrtSubtitle>)subtitles).Clear());
    }

    [Theory]
    [InlineData("")]
    [InlineData("\n\n")]
    [InlineData("00:00:01,000 --> 00:00:02,000\n高")]
    [InlineData("abc\n00:00:01,000 --> 00:00:02,000\n高")]
    [InlineData("1")]
    [InlineData("1\n高")]
    [InlineData("1\n00:00:01.000 --> 00:00:02.000\n高")]
    [InlineData("1\n00:60:01,000 --> 00:00:02,000\n高")]
    [InlineData("1\n00:00:01,000 --> 00:00:02,000")]
    [InlineData("1\n00:00:01,000 --> 00:00:02,000\n\n")]
    [InlineData("1\n00:00:01,000 --> 00:00:02,000\n高\n2\n00:00:03,000 --> 00:00:04,000\n会")]
    [InlineData("1\n00:00:01,000 --> 00:00:02,000\n高\n\n1\n00:00:03,000 --> 00:00:04,000\n会")]
    public void Run_InvalidSrt_Returns2WithStderrAndNoPartialJson(string text)
    {
        Assert.Throws<FormatException>(() => SrtParser.Parse(text));
        WithFile(Encoding.UTF8.GetBytes(text), file =>
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(2, CliApplication.Run(new[] { "joyo", file, "--json" }, output, error));
            Assert.Equal("", output.ToString());
            Assert.Contains("Invalid SRT at source line", error.ToString());
        });
    }

    [Theory]
    [InlineData("check", "高会", 0)]
    [InlineData("check", "議", 1)]
    [InlineData("check", "髙", 1)]
    [InlineData("joyo", "高会議", 0)]
    [InlineData("joyo", "髙會", 1)]
    public void Run_SrtCommands_DiagnoseOnlyTextAndReturnExpectedExit(string command, string body, int code)
    {
        WithFile(Encoding.UTF8.GetBytes("12\n" + Timing + "\n" + body), file =>
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(code, CliApplication.Run(Args(command, file), output, error));
            Assert.Equal("", error.ToString());
            Assert.Contains(code == 0 ? command == "check" ? "No unlearned kanji found." : "No non-Joyo kanji found."
                : "subtitle=12  time=00:01:15,200-00:01:18,300  line=1  index=0", output.ToString());
            var check = SrtCheck.Analyze(CliInput.Read(file).Subtitles!, command == "check" ? CliCommand.Check : CliCommand.Joyo, KanjiGrade.Grade3);
            Assert.Equal(body, string.Concat(check.Items.Select(x => x.Original)));
        });
    }

    [Theory]
    [InlineData("check", false)]
    [InlineData("check", true)]
    [InlineData("joyo", false)]
    [InlineData("joyo", true)]
    public void Run_SrtJson_MapsPositionsAndAggregatesAcrossSubtitles(string command, bool unique)
    {
        string text = "12\n" + Timing + "\n明日は\n議髙\n\n27\n" + Timing + "\n議議髙";
        WithFile(Encoding.UTF8.GetBytes(text), file =>
        {
            var args = Args(command, file).Concat(unique ? new[] { "--json", "--unique" } : new[] { "--json" }).ToArray();
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(1, CliApplication.Run(args, output, error));
            Assert.Equal("", error.ToString());
            using var document = JsonDocument.Parse(output.ToString());
            var root = document.RootElement;
            Assert.False(root.GetProperty("hasInvalidUtf16").GetBoolean());
            if (command == "check")
            {
                Assert.Equal(3, root.GetProperty("targetGrade").GetInt32());
                Assert.Equal(4, root.GetProperty("highestEducationKanjiGrade").GetInt32());
                Assert.True(root.GetProperty("hasNonEducationKanji").GetBoolean());
                Assert.False(root.GetProperty("containsOnlyLearnedKanji").GetBoolean());
            }
            else
            {
                Assert.True(root.GetProperty("hasNonJoyoKanji").GetBoolean());
                Assert.False(root.GetProperty("containsOnlyJoyoKanji").GetBoolean());
            }
            var rows = root.GetProperty(command == "check" ? "unlearned" : "nonJoyo").EnumerateArray().ToArray();
            Assert.Equal(command == "check" ? unique ? 2 : 5 : unique ? 1 : 2, rows.Length);
            var first = rows[0];
            Assert.Equal(command == "check" ? "議" : "髙", first.GetProperty("text").GetString());
            Assert.Equal(command == "check" ? 4 : 5, first.GetProperty(unique ? "firstUtf16Index" : "utf16Index").GetInt32());
            Assert.Equal(12, first.GetProperty(unique ? "firstSubtitleNumber" : "subtitleNumber").GetInt32());
            Assert.Equal("00:01:15,200", first.GetProperty(unique ? "firstStartTime" : "startTime").GetString());
            Assert.Equal("00:01:18,300", first.GetProperty(unique ? "firstEndTime" : "endTime").GetString());
            Assert.Equal(2, first.GetProperty(unique ? "firstTextLine" : "textLine").GetInt32());
            Assert.Equal(command == "check" ? 0 : 1, first.GetProperty(unique ? "firstUtf16IndexInLine" : "utf16IndexInLine").GetInt32());
            if (unique)
            {
                Assert.Equal(command == "check" ? 3 : 2, first.GetProperty("count").GetInt32());
                Assert.False(first.TryGetProperty("utf16Length", out _));
            }
            else
            {
                Assert.Equal(1, first.GetProperty("utf16Length").GetInt32());
                Assert.Equal(27, rows[^1].GetProperty("subtitleNumber").GetInt32());
            }
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Write_UniquePlain_IncludesFirstSrtLocation(bool joyo)
    {
        var subtitles = SrtParser.Parse("12\n" + Timing + "\n議髙\n\n27\n" + Timing + "\n議髙");
        var output = new StringWriter();
        SrtCheckOutput.Write(SrtCheck.Analyze(subtitles, joyo ? CliCommand.Joyo : CliCommand.Check, KanjiGrade.Grade3), true, false, output);
        Assert.Contains("count=2  firstSubtitle=12", output.ToString());
        Assert.Contains(joyo ? "firstLine=1  firstIndex=1" : "firstLine=1  firstIndex=0", output.ToString());
    }

    [Theory]
    [InlineData(false, "\n")]
    [InlineData(true, "\n")]
    [InlineData(false, "\r\n")]
    [InlineData(true, "\r\n")]
    public void Run_SrtUtf8AndLineEndings_PreservesMultilineIndex(bool bom, string newline)
    {
        byte[] bytes = Encoding.UTF8.GetBytes("1" + newline + Timing + newline + "高" + newline + "會");
        if (bom) bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(bytes).ToArray();
        WithFile(bytes, file =>
        {
            var output = new StringWriter();
            Assert.Equal(1, CliApplication.Run(new[] { "joyo", file, "--json" }, output, new StringWriter()));
            using var json = JsonDocument.Parse(output.ToString());
            var row = json.RootElement.GetProperty("nonJoyo")[0];
            Assert.Equal(1 + newline.Length, row.GetProperty("utf16Index").GetInt32());
            Assert.Equal(2, row.GetProperty("textLine").GetInt32());
            Assert.Equal(0, row.GetProperty("utf16IndexInLine").GetInt32());
        });
    }

    [Fact]
    public void Run_SrtInvalidUtf8_IsReadErrorWithEmptyStdout()
    {
        WithFile(new byte[] { 0xFF }, file =>
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(2, CliApplication.Run(new[] { "joyo", file, "--json" }, output, error));
            Assert.Equal("", output.ToString());
            Assert.Contains("Input is not valid UTF-8", error.ToString());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Analyze_SupplementaryIvsSvsAndTags_PreservesOriginalSourcePositions(bool joyo)
    {
        string text = "7\n" + Timing + "\n<i>髙𠮷不\uFE00学\U000E01EF</i>\n髙";
        var result = SrtCheck.Analyze(SrtParser.Parse(text), joyo ? CliCommand.Joyo : CliCommand.Check, KanjiGrade.Grade3);
        Assert.Equal(new[] { "髙", "𠮷", "不\uFE00", "学\U000E01EF", "髙" }, result.Problems.Select(x => x.Original));
        Assert.Equal(new[] { 3, 4, 6, 8, 0 }, result.Problems.Select(x => x.Position.Utf16IndexInLine));
        Assert.Equal(new[] { 1, 2, 2, 3, 1 }, result.Problems.Select(x => x.Utf16Length));
        Assert.Equal(new[] { 1, 1, 1, 1, 2 }, result.Problems.Select(x => x.Position.TextLine));
        string[] lines = text.Split('\n');
        foreach (var item in result.Items)
            Assert.Equal(item.Original, (lines[item.Position.SourceLineNumber - 1] + "\n").Substring(item.Position.Utf16IndexInLine, item.Utf16Length));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Analyze_InvalidUtf16_MapsPositionAndPrioritizesExit2(bool joyo)
    {
        string text = "3\n" + Timing + "\n高\n" + new string('\uD800', 1) + "髙";
        var check = SrtCheck.Analyze(SrtParser.Parse(text), joyo ? CliCommand.Joyo : CliCommand.Check, KanjiGrade.Grade3);
        var invalid = Assert.Single(check.Items, x => x.Reason == "InvalidUtf16");
        Assert.Equal(2, invalid.Utf16Index);
        Assert.Equal(1, invalid.Utf16Length);
        Assert.Equal(2, invalid.Position.TextLine);
        Assert.Equal(0, invalid.Position.Utf16IndexInLine);
        Assert.Equal(4, invalid.Position.SourceLineNumber);
        Assert.Equal(2, check.ExitCode);
        Assert.True(check.HasInvalidUtf16);
        Assert.DoesNotContain(invalid, check.Problems);
        var output = new StringWriter();
        SrtCheckOutput.Write(check, false, true, output);
        using var json = JsonDocument.Parse(output.ToString());
        Assert.True(json.RootElement.GetProperty("hasInvalidUtf16").GetBoolean());
        Assert.Single(check.Problems);
    }

    [Fact]
    public void Analyze_EducationSummary_UsesHighestGradeAcrossAllSubtitles()
    {
        var check = SrtCheck.Analyze(SrtParser.Parse("1\n" + Timing + "\n胃\n\n2\n" + Timing + "\n髙日"), CliCommand.Check, KanjiGrade.Grade3);
        Assert.Equal(KanjiGrade.Grade6, check.HighestEducationKanjiGrade);
        Assert.True(check.HasNonTableKanji);
        Assert.False(check.ContainsOnlyAllowedKanji);
    }

    private static string[] Args(string command, string file) => command == "check"
        ? new[] { command, file, "--grade", "3" } : new[] { command, file };

    private static void WithFile(byte[] bytes, Action<string> test)
    {
        // 大文字拡張子も、入力選択・引数検証の両方で受理されることを確認します。
        string file = Path.Combine(Path.GetTempPath(), "kanjivariants " + Guid.NewGuid() + ".SRT");
        try { File.WriteAllBytes(file, bytes); test(file); }
        finally { File.Delete(file); }
    }
}
