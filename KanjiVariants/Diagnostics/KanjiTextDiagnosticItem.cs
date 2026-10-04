// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>元の文字列中の一論理単位についての診断結果。基底文字と直後のVSは登録有無によらず同じ単位です。</summary>
public sealed class KanjiTextDiagnosticItem
{
    /// <summary>元の単位の文字列。不正UTF-16も修正せず保持します。</summary>
    public string Original { get; }
    /// <summary>入力文字列内の0始まりのUTF-16符号単位位置。</summary>
    public int Utf16Index { get; }
    /// <summary>元の単位のUTF-16符号単位数。Original.Lengthと一致します。</summary>
    public int Utf16Length { get; }
    /// <summary>置換前の単位が対象集合でそのまま使用可能かどうか。</summary>
    public bool IsSupported { get; }
    /// <summary>KanjiText.Replaceが実際に採用する置換文字列。元の単位が使用可能、または置換不可の場合はnullです。</summary>
    public string? Replacement { get; }
    /// <summary>使用可能性、採用した置換経路、または置換できない理由。</summary>
    public KanjiDiagnosticReason Reason { get; }

    internal KanjiTextDiagnosticItem(string original, int index, string? replacement, KanjiDiagnosticReason reason)
    {
        Original = original;
        Utf16Index = index;
        Utf16Length = original.Length;
        IsSupported = reason == KanjiDiagnosticReason.Supported;
        Replacement = replacement;
        Reason = reason;
    }
}
