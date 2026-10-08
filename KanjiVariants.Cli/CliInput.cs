// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants.Cli;

internal sealed record CliInput(string Text, IReadOnlyList<SrtSubtitle>? Subtitles)
{
    // 形式選択を一か所に集め、診断処理と各形式のパーサーを分離します。
    internal static CliInput Read(string file)
    {
        string text = Utf8TextInput.Read(file);
        return Path.GetExtension(file).ToLowerInvariant() switch
        {
            ".txt" => new CliInput(text, null),
            ".srt" => new CliInput(text, SrtParser.Parse(text)),
            _ => throw new ArgumentException("Only .txt and .srt files are supported."),
        };
    }
}
