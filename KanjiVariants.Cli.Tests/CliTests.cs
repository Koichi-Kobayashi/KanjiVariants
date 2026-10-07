// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;
using System.Text.Json;
using KanjiVariants.Cli;
using Xunit;

namespace KanjiVariants.Cli.Tests;

public sealed class CliTests
{
    [Theory]
    [InlineData("1", KanjiGrade.Grade1)]
    [InlineData("2", KanjiGrade.Grade2)]
    [InlineData("3", KanjiGrade.Grade3)]
    [InlineData("4", KanjiGrade.Grade4)]
    [InlineData("5", KanjiGrade.Grade5)]
    [InlineData("6", KanjiGrade.Grade6)]
    public void Parse_Grade1Through6_ReturnsExpectedGrade(string number, KanjiGrade expected)
    {
        var options = CliOptions.Parse(new[] { "check", "sample.TXT", "--json", "--grade", number, "--unique" });
        Assert.Equal(expected, options.Grade);
        Assert.True(options.Unique);
        Assert.True(options.Json);
    }

    [Theory]
    [InlineData("")]
    [InlineData("other sample.txt --grade 3")]
    [InlineData("check")]
    [InlineData("check --grade 3")]
    [InlineData("check sample.txt")]
    [InlineData("check sample.txt --grade")]
    [InlineData("check sample.txt --grade 0")]
    [InlineData("check sample.txt --grade 7")]
    [InlineData("check sample.txt --grade -1")]
    [InlineData("check sample.txt --grade x")]
    [InlineData("check sample.txt --grade 3.0")]
    [InlineData("check sample.txt --grade --json")]
    [InlineData("check sample.txt --grade 3 --unknown")]
    [InlineData("check sample.txt --grade 3 --grade 4")]
    [InlineData("check sample.txt --grade 3 --unique --unique")]
    [InlineData("check sample.txt --grade 3 --json --json")]
    [InlineData("check sample.txt extra.txt --grade 3")]
    [InlineData("check sample.csv --grade 3")]
    public void Run_InvalidArguments_Returns2AndWritesOnlyToStderr(string command)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        int code = CliApplication.Run(command.Split(' ', StringSplitOptions.RemoveEmptyEntries), output, error);
        Assert.Equal(2, code);
        Assert.Equal("", output.ToString());
        Assert.Contains(CliOptions.Usage, error.ToString());
    }

    [Fact]
    public void Run_MissingFile_Returns2WithoutJsonOnStdout()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
        Assert.Equal(2, CliApplication.Run(new[] { "check", file, "--grade", "3", "--json" }, output, error));
        Assert.Equal("", output.ToString());
        Assert.Contains("Error: Could not read file:", error.ToString());
    }

    [Theory]
    [InlineData("学校", KanjiGrade.Grade1, false, 0)]
    [InlineData("議", KanjiGrade.Grade4, false, 1)]
    [InlineData("学校催", KanjiGrade.Grade1, true, 1)]
    [InlineData("あいうえお", null, false, 0)]
    [InlineData("", null, false, 0)]
    [InlineData("𠮷", null, true, 1)]
    [InlineData("学\U000E0100", null, true, 1)]
    [InlineData("不\uFE00", null, true, 1)]
    public void Run_NormalDiagnosis_ReturnsExpectedSummaryAndExitCode(
        string text, KanjiGrade? highestGrade, bool nonEducation, int expectedCode)
    {
        WithFile(new UTF8Encoding(false).GetBytes(text), file =>
        {
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(expectedCode, CliApplication.Run(new[] { "check", file, "--grade", "3" }, output, error));
            Assert.Equal("", error.ToString());
            Assert.Contains("Target grade: Grade3", output.ToString());
            Assert.Contains($"Highest education-kanji grade: {highestGrade?.ToString() ?? "-"}", output.ToString());
            Assert.Contains($"Contains non-education kanji: {(nonEducation ? "Yes" : "No")}", output.ToString());
            if (expectedCode == 0)
                Assert.Contains("No unlearned kanji found.", output.ToString());
            else
                Assert.Contains("Unlearned kanji:", output.ToString());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Write_TextOutput_UniqueControlsGroupingAndPreservesFirstOccurrenceOrder(bool unique)
    {
        var output = new StringWriter();
        CheckOutput.Write(TextCheck.Analyze("議催議", KanjiGrade.Grade3), unique, false, output);
        string expected = unique
            ? "議  Grade4  count=2" + Environment.NewLine + "催  NotEducationKanji  count=1"
            : "議  Grade4  index=0" + Environment.NewLine + "催  NotEducationKanji  index=1"
                + Environment.NewLine + "議  Grade4  index=2";
        Assert.Contains(expected, output.ToString());
        Assert.DoesNotContain(unique ? "index=" : "count=", output.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Run_JsonOutput_IsValidJsonWithExpectedSchemaAndUtf16Positions(bool unique)
    {
        WithFile(Encoding.UTF8.GetBytes("A𠮷議催議学\U000E0100不\uFE00"), file =>
        {
            var args = new List<string> { "check", file, "--grade", "3", "--json" };
            if (unique) args.Add("--unique");
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(1, CliApplication.Run(args.ToArray(), output, error));
            Assert.Equal("", error.ToString());
            using var json = JsonDocument.Parse(output.ToString());
            var root = json.RootElement;
            Assert.Equal(3, root.GetProperty("targetGrade").GetInt32());
            Assert.Equal(4, root.GetProperty("highestEducationKanjiGrade").GetInt32());
            Assert.True(root.GetProperty("hasNonEducationKanji").GetBoolean());
            Assert.False(root.GetProperty("containsOnlyLearnedKanji").GetBoolean());
            Assert.False(root.GetProperty("hasInvalidUtf16").GetBoolean());
            var rows = root.GetProperty("unlearned").EnumerateArray().ToArray();
            Assert.Equal(unique ? 5 : 6, rows.Length);
            Assert.Equal("𠮷", rows[0].GetProperty("text").GetString());
            Assert.Equal(JsonValueKind.Null, rows[0].GetProperty("grade").ValueKind);
            Assert.Equal("NotEducationKanji", rows[0].GetProperty("reason").GetString());
            Assert.Equal(1, rows[0].GetProperty(unique ? "firstUtf16Index" : "utf16Index").GetInt32());
            Assert.Equal("議", rows[1].GetProperty("text").GetString());
            Assert.Equal(4, rows[1].GetProperty("grade").GetInt32());
            Assert.Equal("LearnedInLaterGrade", rows[1].GetProperty("reason").GetString());
            if (unique)
            {
                Assert.Equal(2, rows[1].GetProperty("count").GetInt32());
                Assert.Equal(3, rows[1].GetProperty("firstUtf16Index").GetInt32());
                Assert.False(rows[1].TryGetProperty("utf16Index", out _));
            }
            else
            {
                Assert.Equal(2, rows[0].GetProperty("utf16Length").GetInt32());
                Assert.Equal(3, rows[4].GetProperty("utf16Length").GetInt32());
                Assert.Equal(2, rows[5].GetProperty("utf16Length").GetInt32());
                Assert.False(rows[1].TryGetProperty("count", out _));
            }
        });
    }

    [Fact]
    public void Write_JsonWithoutEducationKanji_EmitsNullHighestGradeAndEmptyList()
    {
        var output = new StringWriter();
        CheckOutput.Write(TextCheck.Analyze("abc", KanjiGrade.Grade3), false, true, output);
        using var json = JsonDocument.Parse(output.ToString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("highestEducationKanjiGrade").ValueKind);
        Assert.Empty(json.RootElement.GetProperty("unlearned").EnumerateArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Read_Utf8WithOrWithoutBom_PreservesTextAndDoesNotTreatBomAsInput(bool bom)
    {
        byte[] bytes = Encoding.UTF8.GetBytes("学校議");
        if (bom) bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(bytes).ToArray();
        WithFile(bytes, file =>
        {
            Assert.Equal("学校議", Utf8TextInput.Read(file));
            var output = new StringWriter();
            Assert.Equal(1, CliApplication.Run(new[] { "check", file, "--grade", "3" }, output, new StringWriter()));
            Assert.Contains("議  Grade4  index=2", output.ToString());
        });
    }

    [Theory]
    [InlineData("\r\n", 3)]
    [InlineData("\n", 2)]
    public void Read_LineEndings_ArePreservedInDiagnosticPositions(string newline, int index)
    {
        string text = "日" + newline + "議";
        WithFile(Encoding.UTF8.GetBytes(text), file =>
        {
            string read = Utf8TextInput.Read(file);
            Assert.Equal(text, read);
            Assert.Equal(index, Assert.Single(TextCheck.Analyze(read, KanjiGrade.Grade3).Diagnosis.UnlearnedItems).Utf16Index);
        });
    }

    [Theory]
    [InlineData(new byte[] { 0xC3, 0x28 })]
    [InlineData(new byte[] { 0xED, 0xA0, 0x80 })]
    [InlineData(new byte[] { 0xF0, 0x9F })]
    [InlineData(new byte[] { 0xFF, 0xFE, 0x61, 0x00 })]
    public void Run_InvalidUtf8OrUtf16File_Returns2WithoutReplacementDecoding(byte[] bytes)
    {
        WithFile(bytes, file =>
        {
            Assert.Throws<DecoderFallbackException>(() => Utf8TextInput.Read(file));
            var output = new StringWriter();
            var error = new StringWriter();
            Assert.Equal(2, CliApplication.Run(new[] { "check", file, "--grade", "3", "--json" }, output, error));
            Assert.Equal("", output.ToString());
            Assert.Contains("Input is not valid UTF-8", error.ToString());
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Analyze_InvalidUtf16_Returns2EvenWhenUnlearnedKanjiAlsoExist(bool includeUnlearned)
    {
        string text = new string('\uD800', 1) + (includeUnlearned ? "議" : "");
        var check = TextCheck.Analyze(text, KanjiGrade.Grade3);
        Assert.Equal(2, check.ExitCode);
        Assert.True(check.HasInvalidUtf16);
        Assert.Equal(includeUnlearned ? 1 : 0, check.Diagnosis.UnlearnedItems.Count);
        var output = new StringWriter();
        CheckOutput.Write(check, false, true, output);
        using var json = JsonDocument.Parse(output.ToString());
        Assert.True(json.RootElement.GetProperty("hasInvalidUtf16").GetBoolean());
        var plain = new StringWriter();
        CheckOutput.Write(check, false, false, plain);
        Assert.Contains("Invalid UTF-16 detected in input.", plain.ToString());
    }

    private static void WithFile(byte[] bytes, Action<string> test)
    {
        // 空白を含むパスも一つの引数として扱えることを各ファイルテストで確認します。
        string file = Path.Combine(Path.GetTempPath(), "kanjivariants cli " + Guid.NewGuid() + ".txt");
        try
        {
            File.WriteAllBytes(file, bytes);
            test(file);
        }
        finally
        {
            File.Delete(file);
        }
    }
}
