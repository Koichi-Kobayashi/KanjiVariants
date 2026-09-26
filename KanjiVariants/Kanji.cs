// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>一文字の漢字の代替候補と文字集合への所属を調べます。</summary>
public static class Kanji
{
    /// <summary>MJ縮退マップにある候補と、明示的に許可された基底文字から使用可能な文字を重複なく取得します。</summary>
    /// <param name="character">候補を調べる漢字。登録済みの IVS/SVS も指定できます。</param>
    /// <param name="characterSet">候補の所属を確認する文字集合。</param>
    /// <param name="options">登録済み IVS/SVS の基底文字へフォールバックするかどうか。</param>
    /// <returns>使用可能な候補の一覧。候補がない場合は空の一覧です。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> に未定義の値が含まれる場合。</exception>
    public static IReadOnlyList<KanjiCharacter> GetAlternatives(
        KanjiCharacter character, CharacterSet characterSet, KanjiFallbackOptions options = KanjiFallbackOptions.None)
    {
        Lookup.Validate(characterSet);
        Lookup.Validate(options);
        int baseCp = character.BaseCharacter.Value;
        int? selectorCp = character.VariationSelector?.Value;
        bool addBase = Lookup.CanFallbackToBase(baseCp, selectorCp, characterSet, options);
        // IVS/SVS は基底文字と分けず、登録済みの組み合わせ全体で検索します。
        int entry = Lookup.FindEntry(baseCp, selectorCp);
        if (entry < 0 && !addBase)
            return Array.Empty<KanjiCharacter>();
        int start = entry < 0 ? 0 : GeneratedData.AlternativeStarts[entry];
        int count = entry < 0 ? 0 : GeneratedData.AlternativeCounts[entry];
        var result = new List<KanjiCharacter>(count + (addBase ? 1 : 0));
        bool hasBase = false;
        for (int i = start; i < start + count; i++)
        {
            int cp = GeneratedData.AlternativeCodePoints[i];
            // 候補はデータ側で重複排除済みですが、対象文字集合に含まれる候補だけを返します。
            if (Lookup.IsSupported(cp, characterSet))
            {
                result.Add(KanjiCharacter.FromScalar(cp));
                hasBase |= cp == baseCp;
            }
        }
        // MJ候補に同じ文字があれば追加せず、明示的に許可された字形指定の縮退だけを補います。
        if (addBase && !hasBase)
            result.Add(KanjiCharacter.FromScalar(baseCp));
        return result.AsReadOnly();
    }

    /// <summary>文字列で指定した漢字の代替候補を取得します。</summary>
    /// <param name="character">認識可能な漢字一文字または登録済み IVS/SVS。</param>
    /// <param name="characterSet">候補の所属を確認する文字集合。</param>
    /// <param name="options">登録済み IVS/SVS の基底文字へフォールバックするかどうか。</param>
    /// <returns>使用可能な候補の一覧。候補がない場合は空の一覧です。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="character"/> が null の場合。</exception>
    /// <exception cref="FormatException"><paramref name="character"/> が漢字一文字または登録済み IVS/SVS でない場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> に未定義の値が含まれる場合。</exception>
    public static IReadOnlyList<KanjiCharacter> GetAlternatives(
        string character, CharacterSet characterSet, KanjiFallbackOptions options = KanjiFallbackOptions.None) =>
        GetAlternatives(KanjiCharacter.Parse(character), characterSet, options);

    /// <summary>MJ縮退マップの一意な変換先を優先し、明示的に許可された場合は登録済み IVS/SVS の基底文字へフォールバックします。</summary>
    /// <param name="character">変換先を調べる漢字。登録済みの IVS/SVS も指定できます。</param>
    /// <param name="characterSet">変換先の所属を確認する文字集合。</param>
    /// <param name="alternative">成功時は変換先。それ以外は既定値です。</param>
    /// <param name="options">登録済み IVS/SVS の基底文字へフォールバックするかどうか。</param>
    /// <returns>文字集合内の変換先が得られた場合は true。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> に未定義の値が含まれる場合。</exception>
    public static bool TryGetAlternative(KanjiCharacter character, CharacterSet characterSet,
        out KanjiCharacter alternative, KanjiFallbackOptions options = KanjiFallbackOptions.None)
    {
        Lookup.Validate(characterSet);
        Lookup.Validate(options);
        int baseCp = character.BaseCharacter.Value;
        int? selectorCp = character.VariationSelector?.Value;
        int entry = Lookup.FindEntry(baseCp, selectorCp);
        // 複数候補から選ばず、公式の「一意な変換表」で定義された結果だけを使います。
        int cp = entry < 0 ? 0 : GeneratedData.UniqueAlternatives[entry];
        if (cp != 0 && Lookup.IsSupported(cp, characterSet))
        {
            alternative = KanjiCharacter.FromScalar(cp);
            return true;
        }
        if (Lookup.CanFallbackToBase(baseCp, selectorCp, characterSet, options))
        {
            alternative = KanjiCharacter.FromScalar(baseCp);
            return true;
        }
        alternative = default;
        return false;
    }

    /// <summary>文字列で指定した漢字について、公式の一意な変換先または許可された基底文字を取得します。</summary>
    /// <param name="character">認識可能な漢字一文字または登録済み IVS/SVS。</param>
    /// <param name="characterSet">変換先の所属を確認する文字集合。</param>
    /// <param name="alternative">成功時は変換先。それ以外は既定値です。</param>
    /// <param name="options">登録済み IVS/SVS の基底文字へフォールバックするかどうか。</param>
    /// <returns>文字集合内の変換先が得られた場合は true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="character"/> が null の場合。</exception>
    /// <exception cref="FormatException"><paramref name="character"/> が漢字一文字または登録済み IVS/SVS でない場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="options"/> に未定義の値が含まれる場合。</exception>
    public static bool TryGetAlternative(string character, CharacterSet characterSet,
        out KanjiCharacter alternative, KanjiFallbackOptions options = KanjiFallbackOptions.None) =>
        TryGetAlternative(KanjiCharacter.Parse(character), characterSet, out alternative, options);

    /// <summary>漢字表現そのものが指定文字集合に含まれるかを判定します。</summary>
    /// <param name="character">所属を調べる漢字。Variation Selector 付きの表現は JIS X 0208 では false です。</param>
    /// <param name="characterSet">所属を確認する文字集合。</param>
    /// <returns>漢字表現が文字集合に含まれる場合は true。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static bool IsSupported(KanjiCharacter character, CharacterSet characterSet)
    {
        Lookup.Validate(characterSet);
        return character.VariationSelector is null && Lookup.IsSupported(character.BaseCharacter.Value, characterSet);
    }

    /// <summary>文字列で指定した漢字の所属を判定します。</summary>
    /// <param name="character">認識可能な漢字一文字または登録済み IVS/SVS。</param>
    /// <param name="characterSet">所属を確認する文字集合。</param>
    /// <returns>漢字表現が文字集合に含まれる場合は true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="character"/> が null の場合。</exception>
    /// <exception cref="FormatException"><paramref name="character"/> が漢字一文字または登録済み IVS/SVS でない場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static bool IsSupported(string character, CharacterSet characterSet) =>
        IsSupported(KanjiCharacter.Parse(character), characterSet);
}
