// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>MJ文字情報一覧表に収録された人名用漢字を参照します。</summary>
public static class JinmeiyoKanji
{
    // 一覧は一度だけ構築し、呼び出し元から変更できない形で共有します。
    private static readonly IReadOnlyList<KanjiCharacter> All = Array.AsReadOnly(
        Array.ConvertAll(GeneratedJinmeiyoKanjiData.CodePoints, KanjiCharacter.FromScalar));

    /// <summary>指定した文字そのものが人名用漢字863字に含まれるかを判定します。常用漢字は自動的には含めません。</summary>
    public static bool IsJinmeiyoKanji(KanjiCharacter character) =>
        !character.HasVariationSelector &&
        Array.BinarySearch(GeneratedJinmeiyoKanjiData.CodePoints, character.BaseCharacter.Value) >= 0;

    /// <summary>文字列が人名用漢字863字の一字かを判定します。無効な文字列やIVS/SVSは false です。</summary>
    public static bool IsJinmeiyoKanji(string? character) =>
        KanjiCharacter.TryParse(character, out var parsed) && IsJinmeiyoKanji(parsed);

    /// <summary>人名用漢字863字だけを、共有の読み取り専用一覧で返します。</summary>
    public static IReadOnlyList<KanjiCharacter> GetAll() => All;

    /// <summary>指定した文字そのものが常用漢字または人名用漢字かを判定します。仮名やIVS/SVSは含めません。</summary>
    public static bool IsNameUsableKanji(KanjiCharacter character) =>
        JoyoKanji.IsJoyo(character) || IsJinmeiyoKanji(character);

    /// <summary>文字列が常用漢字または人名用漢字の一字かを判定します。無効な文字列やIVS/SVSは false です。</summary>
    public static bool IsNameUsableKanji(string? character) =>
        KanjiCharacter.TryParse(character, out var parsed) && IsNameUsableKanji(parsed);
}
