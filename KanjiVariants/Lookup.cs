// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

internal static class Lookup
{
    internal static bool IsSupported(int codePoint, CharacterSet characterSet)
    {
        Validate(characterSet);
        // JIS X 0208 は BMP 内なので、生成済みフラグ配列を直接参照します。
        return codePoint < GeneratedData.JisX0208Flags.Length && GeneratedData.JisX0208Flags[codePoint] != 0;
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
        if (characterSet != CharacterSet.JisX0208)
            throw new ArgumentOutOfRangeException(nameof(characterSet));
    }

    internal static void Validate(KanjiFallbackOptions options)
    {
        if ((options & ~KanjiFallbackOptions.AllowVariationSelectorFallback) != 0)
            throw new ArgumentOutOfRangeException(nameof(options));
    }
}
