// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>入力中の一論理単位についての常用漢字診断結果。</summary>
public sealed class JoyoKanjiDiagnosticItem
{
    /// <summary>元の文字列。直後のVSと不正UTF-16も保持し、正規化や異体字変換を行いません。</summary>
    public string Original { get; }
    /// <summary>入力文字列内の0始まりUTF-16コード単位位置。</summary>
    public int Utf16Index { get; }
    /// <summary>この項目のUTF-16コード単位数。Original.Lengthと一致します。</summary>
    public int Utf16Length { get; }
    /// <summary>本表字・非常用漢字・漢字以外・不正UTF-16の判定理由。</summary>
    public JoyoKanjiDiagnosticReason Reason { get; }
    /// <summary>常用漢字表の本表字そのものの場合だけtrue。漢字以外もfalseです。</summary>
    public bool IsJoyoKanji => Reason == JoyoKanjiDiagnosticReason.JoyoKanji;

    internal JoyoKanjiDiagnosticItem(string original, int index, JoyoKanjiDiagnosticReason reason)
    {
        Original = original;
        Utf16Index = index;
        Utf16Length = original.Length;
        Reason = reason;
    }
}
