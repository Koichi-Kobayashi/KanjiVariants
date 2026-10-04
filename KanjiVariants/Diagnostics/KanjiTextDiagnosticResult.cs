// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文字列全体の、置換前の文字集合適合性と各単位の置換判断。</summary>
public sealed class KanjiTextDiagnosticResult
{
    /// <summary>診断した入力stringそのもの。コピーや正規化を行いません。</summary>
    public string Text { get; }
    /// <summary>診断対象の文字集合。</summary>
    public CharacterSet CharacterSet { get; }
    /// <summary>診断時に指定したフォールバック設定。</summary>
    public KanjiFallbackOptions Options { get; }
    /// <summary>置換前の入力全体がそのまま使用可能かどうか。空文字はtrueです。</summary>
    public bool IsSupported { get; }
    /// <summary>入力を隙間・重複なく覆う読み取り専用一覧。使用可能な単位や不正UTF-16も含みます。</summary>
    public IReadOnlyList<KanjiTextDiagnosticItem> Items { get; }

    internal KanjiTextDiagnosticResult(string text, CharacterSet characterSet, KanjiFallbackOptions options,
        bool supported, IReadOnlyList<KanjiTextDiagnosticItem> items)
    {
        Text = text;
        CharacterSet = characterSet;
        Options = options;
        IsSupported = supported;
        Items = items;
    }
}
