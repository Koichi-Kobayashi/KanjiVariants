// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文字列全体について、指定小学学年までの教育漢字で構成されているかの診断結果。</summary>
public sealed class EducationKanjiTextDiagnosticResult
{
    /// <summary>診断した入力stringそのもの。コピーや正規化を行いません。</summary>
    public string Text { get; }
    /// <summary>この学年までの配当漢字を既習として扱います。</summary>
    public KanjiGrade TargetGrade { get; }
    /// <summary>未習の漢字や不正UTF-16がなければtrue。漢字以外は判定対象外で、空文字もtrueです。</summary>
    public bool ContainsOnlyLearnedKanji { get; }
    /// <summary>教育漢字として判定できた項目の最高配当学年。教育漢字がなければnullです。</summary>
    /// <remarks>TargetGradeとは独立した値です。配当表外漢字、漢字以外、不正UTF-16は計算対象外で、文章全体がこの学年までの教育漢字だけで構成されることを保証しません。</remarks>
    public KanjiGrade? HighestEducationKanjiGrade { get; }
    /// <summary>NotEducationKanjiの項目が一つでもあればtrue。漢字以外や不正UTF-16は含めません。</summary>
    public bool HasNonEducationKanji { get; }
    /// <summary>入力全体を隙間・重複なく覆う、入力順の読み取り専用一覧。</summary>
    public IReadOnlyList<EducationKanjiTextDiagnosticItem> Items { get; }
    /// <summary>未習漢字だけの入力順の読み取り専用一覧。後の学年の教育漢字と配当表外の漢字を含み、不正UTF-16は含めません。Items内の同じ項目を共有します。</summary>
    public IReadOnlyList<EducationKanjiTextDiagnosticItem> UnlearnedItems { get; }

    internal EducationKanjiTextDiagnosticResult(string text, KanjiGrade targetGrade, bool allLearned,
        IReadOnlyList<EducationKanjiTextDiagnosticItem> items, IReadOnlyList<EducationKanjiTextDiagnosticItem> unlearned,
        KanjiGrade? highestEducationKanjiGrade, bool hasNonEducationKanji)
    {
        Text = text;
        TargetGrade = targetGrade;
        ContainsOnlyLearnedKanji = allLearned;
        HighestEducationKanjiGrade = highestEducationKanjiGrade;
        HasNonEducationKanji = hasNonEducationKanji;
        Items = items;
        UnlearnedItems = unlearned;
    }
}
