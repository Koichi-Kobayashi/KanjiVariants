// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

[Flags]
internal enum CharacterSetFlags : byte
{
    None = 0,
    JisX0208 = 1 << 0,
    JisX0213Plane1 = 1 << 1,
    JisX0213Plane2 = 1 << 2,
}

internal static class Lookup
{
    internal static bool IsSupported(int codePoint, CharacterSet characterSet)
    {
        // 通常のコードポイントだけに付けた所属bitを参照し、各公開文字集合を共通の規則で判定します。
        CharacterSetFlags flags = (uint)codePoint < (uint)GeneratedData.BmpCharacterSetFlags.Length
            ? (CharacterSetFlags)GeneratedData.BmpCharacterSetFlags[codePoint]
            : (uint)(codePoint - 0x20000) < (uint)GeneratedData.SupplementaryCharacterSetFlags.Length
                ? (CharacterSetFlags)GeneratedData.SupplementaryCharacterSetFlags[codePoint - 0x20000]
                : CharacterSetFlags.None;
        return characterSet switch
        {
            CharacterSet.JisX0208 => (flags & CharacterSetFlags.JisX0208) != 0,
            CharacterSet.JisX0213Plane1 => (flags & CharacterSetFlags.JisX0213Plane1) != 0,
            CharacterSet.JisX0213Plane2 => (flags & CharacterSetFlags.JisX0213Plane2) != 0,
            CharacterSet.JisX0213 => (flags & (CharacterSetFlags.JisX0213Plane1 | CharacterSetFlags.JisX0213Plane2)) != 0,
            _ => throw new ArgumentOutOfRangeException(nameof(characterSet)),
        };
    }

    // 文字集合への登録有無とは分け、漢字範囲またはMJ文字情報のある文字を解析対象にします。
    internal static bool IsRecognizedKanji(int cp) =>
        (cp >= 0x3400 && cp <= 0x4DBF) ||
        (cp >= 0x4E00 && cp <= 0x9FFF) ||
        (cp >= 0xF900 && cp <= 0xFAFF) ||
        (cp >= 0x20000 && cp <= 0x323AF) ||
        FindEntry(cp, null) >= 0;

    internal static bool IsVariationSelector(int cp) =>
        (cp >= 0xFE00 && cp <= 0xFE0F) || (cp >= 0xE0100 && cp <= 0xE01EF);

    internal static bool IsRegisteredVariation(int baseCp, int selectorCp) =>
        Array.BinarySearch(GeneratedData.ValidVariationKeys, CreateKey(baseCp, selectorCp)) >= 0;

    // VS 除去は字形指定を失うため、明示的な許可・正式登録・基底文字の所属をすべて確認します。
    internal static bool CanFallbackToBase(int baseCp, int? selectorCp, CharacterSet characterSet, KanjiFallbackOptions options) =>
        (options & KanjiFallbackOptions.AllowVariationSelectorFallback) != 0 &&
        selectorCp is int selector &&
        IsRegisteredVariation(baseCp, selector) &&
        IsSupported(baseCp, characterSet);

    internal static int FindEntry(int baseCp, int? selectorCp)
    {
        if (selectorCp is null)
        {
            // 通常文字はコードポイントを添字にした直接参照表で検索します。
            if (baseCp < GeneratedData.BmpEntryIndices.Length)
                return GeneratedData.BmpEntryIndices[baseCp];
            int supplementaryIndex = baseCp - 0x20000;
            return (uint)supplementaryIndex < (uint)GeneratedData.SupplementaryEntryIndices.Length
                ? GeneratedData.SupplementaryEntryIndices[supplementaryIndex] : -1;
        }
        // IVS/SVS は複合キーを昇順配列から二分探索します。
        var index = Array.BinarySearch(GeneratedData.VariationKeys, CreateKey(baseCp, selectorCp.Value));
        return index >= 0 ? GeneratedData.VariationEntryIndices[index] : -1;
    }

    // Unicode scalar は21ビットなので、基底文字と VS を衝突しない ulong キーにまとめられます。
    internal static ulong CreateKey(int baseCp, int selectorCp) =>
        (uint)baseCp | ((ulong)(uint)selectorCp << 21);

    internal static void Validate(CharacterSet characterSet)
    {
        if ((uint)characterSet > (uint)CharacterSet.JisX0213)
            throw new ArgumentOutOfRangeException(nameof(characterSet));
    }

    internal static void Validate(KanjiFallbackOptions options)
    {
        if ((options & ~KanjiFallbackOptions.AllowVariationSelectorFallback) != 0)
            throw new ArgumentOutOfRangeException(nameof(options));
    }
}
