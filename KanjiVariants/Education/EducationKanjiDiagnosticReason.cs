// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>教育漢字の文章診断における各論理単位の分類。</summary>
public enum EducationKanjiDiagnosticReason
{
    /// <summary>漢字以外の文字。判定対象外で、既習扱いです。</summary>
    NotKanji = 0,
    /// <summary>指定学年までに学習する教育漢字。</summary>
    Learned = 1,
    /// <summary>教育漢字ですが、指定学年より後で学習します。</summary>
    LearnedInLaterGrade = 2,
    /// <summary>学年別漢字配当表に含まれない漢字表現。旧字体・異体字・VS付き表現を自動変換しません。</summary>
    NotEducationKanji = 3,
    /// <summary>不正UTF-16の一符号単位。未習漢字一覧には含めません。</summary>
    InvalidUtf16 = 4,
}
