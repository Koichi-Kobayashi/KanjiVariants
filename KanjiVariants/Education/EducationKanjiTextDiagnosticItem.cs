// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>入力中の一論理単位についての教育漢字診断結果。</summary>
public sealed class EducationKanjiTextDiagnosticItem
{
    /// <summary>元の文字列。基底文字と直後のVSは登録有無によらず一単位として保持します。不正UTF-16も修正しません。</summary>
    public string Original { get; }
    /// <summary>入力文字列内の0始まりUTF-16コード単位位置。</summary>
    public int Utf16Index { get; }
    /// <summary>この項目のUTF-16コード単位数。Original.Lengthと一致します。</summary>
    public int Utf16Length { get; }
    /// <summary>配当表の文字そのものの場合の配当学年。それ以外はnullです。</summary>
    public KanjiGrade? Grade { get; }
    /// <summary>指定学年までに習う漢字かどうか。漢字以外はtrue、不正UTF-16はfalseです。</summary>
    public bool IsLearned { get; }
    /// <summary>既習・未習・対象外・不正UTF-16の判定理由。</summary>
    public EducationKanjiDiagnosticReason Reason { get; }

    internal EducationKanjiTextDiagnosticItem(string original, int index, KanjiGrade? grade, EducationKanjiDiagnosticReason reason)
    {
        Original = original;
        Utf16Index = index;
        Utf16Length = original.Length;
        Grade = grade;
        IsLearned = reason is EducationKanjiDiagnosticReason.NotKanji or EducationKanjiDiagnosticReason.Learned;
        Reason = reason;
    }
}
