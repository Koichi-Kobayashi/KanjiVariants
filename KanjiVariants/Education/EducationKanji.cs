// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>平成29年告示の学年別漢字配当表を参照します。</summary>
public static class EducationKanji
{
    /// <summary>Variation Selectorを伴わない配当表の文字かを判定します。</summary>
    public static bool IsEducationKanji(KanjiCharacter character) => GetGrade(character) is not null;

    /// <summary>文字列が配当表の一字かを判定します。無効な文字列は false です。</summary>
    public static bool IsEducationKanji(string? character) =>
        KanjiCharacter.TryParse(character, out var parsed) && IsEducationKanji(parsed);

    /// <summary>配当学年を取得します。配当表外、旧字体、IVS/SVSは null です。</summary>
    public static KanjiGrade? GetGrade(KanjiCharacter character)
    {
        if (character.HasVariationSelector)
            return null;
        // 検索用のコードポイント昇順配列と学年配列は同じ位置に対応します。
        int index = Array.BinarySearch(GeneratedEducationKanjiData.CodePoints, character.BaseCharacter.Value);
        return index < 0 ? null : (KanjiGrade)GeneratedEducationKanjiData.Grades[index];
    }

    /// <summary>文字列で指定した一字の配当学年を取得します。無効な文字列は null です。</summary>
    public static KanjiGrade? GetGrade(string? character) =>
        KanjiCharacter.TryParse(character, out var parsed) ? GetGrade(parsed) : null;

    /// <summary>指定学年の漢字をTXTの転記順に返します。同じ読み取り専用一覧を共有します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">未定義の学年の場合。</exception>
    public static IReadOnlyList<KanjiCharacter> GetByGrade(KanjiGrade grade)
    {
        int value = (int)grade;
        if (value is < 1 or > 6)
            throw new ArgumentOutOfRangeException(nameof(grade));
        return GeneratedEducationKanjiData.GetByGrade(value);
    }
}
