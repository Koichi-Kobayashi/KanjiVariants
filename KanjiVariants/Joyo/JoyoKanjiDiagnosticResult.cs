// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文字列全体の漢字が常用漢字表の本表字かどうかの診断結果。</summary>
public sealed class JoyoKanjiDiagnosticResult
{
    /// <summary>診断した入力stringそのもの。コピーや正規化を行いません。</summary>
    public string Text { get; }
    /// <summary>入力全体を隙間・重複なく覆う、入力順の読み取り専用一覧。</summary>
    public IReadOnlyList<JoyoKanjiDiagnosticItem> Items { get; }
    /// <summary>NotJoyoKanjiだけの入力順の読み取り専用一覧。Items内の項目を共有し、不正UTF-16は含めません。</summary>
    public IReadOnlyList<JoyoKanjiDiagnosticItem> NonJoyoItems { get; }
    /// <summary>全項目が本表字または漢字以外の場合にtrue。空文字もtrueで、不正UTF-16はfalseです。</summary>
    public bool ContainsOnlyJoyoKanji => !HasNonJoyoKanji && !HasInvalidUtf16;
    /// <summary>NotJoyoKanjiの項目が一つでもあればtrue。不正UTF-16だけではtrueになりません。</summary>
    public bool HasNonJoyoKanji { get; }
    /// <summary>InvalidUtf16の項目が一つでもあればtrue。</summary>
    public bool HasInvalidUtf16 { get; }

    internal JoyoKanjiDiagnosticResult(string text, IReadOnlyList<JoyoKanjiDiagnosticItem> items,
        IReadOnlyList<JoyoKanjiDiagnosticItem> nonJoyoItems, bool hasInvalidUtf16)
    {
        Text = text;
        Items = items;
        NonJoyoItems = nonJoyoItems;
        HasNonJoyoKanji = nonJoyoItems.Count > 0;
        HasInvalidUtf16 = hasInvalidUtf16;
    }
}
