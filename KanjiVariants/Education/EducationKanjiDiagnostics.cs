// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文字列中の漢字が指定した小学学年までに習う教育漢字かを診断します。</summary>
public static class EducationKanjiDiagnostics
{
    /// <summary>入力をUTF-16位置付きの論理単位に分け、指定学年までの教育漢字かを判定します。</summary>
    /// <param name="text">診断する元の文字列。コピーや正規化を行いません。</param>
    /// <param name="targetGrade">この学年までの配当漢字を既習として扱います。</param>
    /// <returns>全項目と、未習の漢字だけの読み取り専用一覧を含む結果。</returns>
    /// <remarks>旧字体・異体字・IVS/SVSを自動変換しません。漢字と直後のVSは登録有無によらず一項目です。漢字以外は判定対象外です。不正UTF-16は例外ではなく診断項目として返します。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/>がnullの場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetGrade"/>が未定義の場合。</exception>
    public static EducationKanjiTextDiagnosticResult AnalyzeText(string text, KanjiGrade targetGrade)
    {
        ArgumentNullException.ThrowIfNull(text);
        int targetOrder = GetGradeOrder(targetGrade);
        if (text.Length == 0)
        {
            var empty = Array.Empty<EducationKanjiTextDiagnosticItem>();
            return new EducationKanjiTextDiagnosticResult(text, targetGrade, true, empty, empty, null, false);
        }

        var items = new List<EducationKanjiTextDiagnosticItem>();
        List<EducationKanjiTextDiagnosticItem>? unlearned = null;
        bool allLearned = true;
        KanjiGrade? highestEducationKanjiGrade = null;
        int highestEducationKanjiGradeOrder = 0;
        bool hasNonEducationKanji = false;
        int offset = 0;
        while (offset < text.Length)
        {
            int start = offset;
            bool valid = KanjiText.TryReadUnit(text, ref offset, out int baseCp, out int? selectorCp);
            KanjiGrade? grade = null;
            EducationKanjiDiagnosticReason reason;
            if (!valid)
                reason = EducationKanjiDiagnosticReason.InvalidUtf16;
            // 踊り字等は、既存の漢字認識やMJ情報の有無によらず判定対象外です。
            else if (baseCp is 0x3005 or 0x3006 or 0x303B || !Lookup.IsRecognizedKanji(baseCp))
                reason = EducationKanjiDiagnosticReason.NotKanji;
            else
            {
                // VSは削除せず、配当表の文字そのものだけを既存APIで照合します。
                if (selectorCp is null)
                    grade = EducationKanji.GetGrade(KanjiCharacter.FromScalar(baseCp));
                if (grade is null)
                {
                    reason = EducationKanjiDiagnosticReason.NotEducationKanji;
                    hasNonEducationKanji = true;
                }
                else
                {
                    int gradeOrder = GetGradeOrder(grade.Value);
                    reason = gradeOrder <= targetOrder ? EducationKanjiDiagnosticReason.Learned
                        : EducationKanjiDiagnosticReason.LearnedInLaterGrade;
                    // 対象学年や配当表外漢字の有無によらず、実際の配当学年を同じ走査で集計します。
                    if (gradeOrder > highestEducationKanjiGradeOrder)
                    {
                        highestEducationKanjiGradeOrder = gradeOrder;
                        highestEducationKanjiGrade = grade;
                    }
                }
            }
            var item = new EducationKanjiTextDiagnosticItem(text.Substring(start, offset - start), start, grade, reason);
            items.Add(item);
            allLearned &= item.IsLearned;
            // 同じ項目を共有し、不正UTF-16を「未習の漢字」一覧に混ぜません。
            if (reason is EducationKanjiDiagnosticReason.LearnedInLaterGrade or EducationKanjiDiagnosticReason.NotEducationKanji)
                (unlearned ??= new List<EducationKanjiTextDiagnosticItem>()).Add(item);
        }
        return new EducationKanjiTextDiagnosticResult(text, targetGrade, allLearned, items.AsReadOnly(),
            unlearned is null ? Array.Empty<EducationKanjiTextDiagnosticItem>() : unlearned.AsReadOnly(),
            highestEducationKanjiGrade, hasNonEducationKanji);
    }

    // enumの数値が変わっても、学年の順序と入力検証を維持します。
    private static int GetGradeOrder(KanjiGrade targetGrade) => targetGrade switch
    {
        KanjiGrade.Grade1 => 1,
        KanjiGrade.Grade2 => 2,
        KanjiGrade.Grade3 => 3,
        KanjiGrade.Grade4 => 4,
        KanjiGrade.Grade5 => 5,
        KanjiGrade.Grade6 => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(targetGrade)),
    };
}
