// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>一文字の漢字の代替候補と文字集合への所属を調べます。</summary>
public static class Kanji
{
    /// <summary>MJ縮退マップにある候補から、指定文字集合で使用できる文字を重複なく取得します。</summary>
    /// <param name="character">候補を調べる漢字。登録済みの IVS/SVS も指定できます。</param>
    /// <param name="characterSet">候補の所属を確認する文字集合。</param>
    /// <returns>使用可能な候補の一覧。候補がない場合は空の一覧です。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static IReadOnlyList<KanjiCharacter> GetAlternatives(KanjiCharacter character, CharacterSet characterSet)
    {
        Lookup.Validate(characterSet);
        // IVS/SVS は基底文字と分けず、登録済みの組み合わせ全体で検索します。
        int entry = Lookup.FindEntry(character.BaseCharacter.Value, character.VariationSelector?.Value);
        if (entry < 0)
            return Array.Empty<KanjiCharacter>();
        int start = GeneratedData.AlternativeStarts[entry];
        int count = GeneratedData.AlternativeCounts[entry];
        var result = new List<KanjiCharacter>(count);
        for (int i = start; i < start + count; i++)
        {
            int cp = GeneratedData.AlternativeCodePoints[i];
            // 候補はデータ側で重複排除済みですが、対象文字集合に含まれる候補だけを返します。
            if (Lookup.IsSupported(cp, characterSet))
                result.Add(KanjiCharacter.FromScalar(cp));
        }
        return result.AsReadOnly();
    }

    /// <summary>文字列で指定した漢字の代替候補を取得します。</summary>
    /// <param name="character">認識可能な漢字一文字または登録済み IVS/SVS。</param>
    /// <param name="characterSet">候補の所属を確認する文字集合。</param>
    /// <returns>使用可能な候補の一覧。候補がない場合は空の一覧です。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="character"/> が null の場合。</exception>
    /// <exception cref="FormatException"><paramref name="character"/> が漢字一文字または登録済み IVS/SVS でない場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static IReadOnlyList<KanjiCharacter> GetAlternatives(string character, CharacterSet characterSet) =>
        GetAlternatives(KanjiCharacter.Parse(character), characterSet);

    /// <summary>MJ縮退マップの一意な変換表に定義された変換先を取得します。</summary>
    /// <param name="character">変換先を調べる漢字。登録済みの IVS/SVS も指定できます。</param>
    /// <param name="characterSet">変換先の所属を確認する文字集合。</param>
    /// <param name="alternative">成功時は変換先。それ以外は既定値です。</param>
    /// <returns>文字集合内の変換先が得られた場合は true。</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static bool TryGetAlternative(KanjiCharacter character, CharacterSet characterSet, out KanjiCharacter alternative)
    {
        Lookup.Validate(characterSet);
        int entry = Lookup.FindEntry(character.BaseCharacter.Value, character.VariationSelector?.Value);
        // 複数候補から選ばず、公式の「一意な変換表」で定義された結果だけを使います。
        int cp = entry < 0 ? 0 : GeneratedData.UniqueAlternatives[entry];
        if (cp != 0 && Lookup.IsSupported(cp, characterSet))
        {
            alternative = KanjiCharacter.FromScalar(cp);
            return true;
        }
        alternative = default;
        return false;
    }

    /// <summary>文字列で指定した漢字について、一意な変換先を取得します。</summary>
    /// <param name="character">認識可能な漢字一文字または登録済み IVS/SVS。</param>
    /// <param name="characterSet">変換先の所属を確認する文字集合。</param>
    /// <param name="alternative">成功時は変換先。それ以外は既定値です。</param>
    /// <returns>文字集合内の変換先が得られた場合は true。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="character"/> が null の場合。</exception>
    /// <exception cref="FormatException"><paramref name="character"/> が漢字一文字または登録済み IVS/SVS でない場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static bool TryGetAlternative(string character, CharacterSet characterSet, out KanjiCharacter alternative) =>
        TryGetAlternative(KanjiCharacter.Parse(character), characterSet, out alternative);

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
