// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants;

internal static class ReadingNormalizer
{
    internal static string Normalize(string value)
    {
        // 正本の読みは変えず、検索キーだけNFCとひらがなへ揃えます。
        string normalized = value;
        // ASCIIと合成済みのかなだけならNFCのままです。分解濁点などを含む入力は
        // Normalizeへ直接渡し、IsNormalizedとの二重のUnicode走査を避けます。
        foreach (char c in value)
        {
            if (c <= '\u007F' || c is >= '\u3041' and <= '\u3096' || c is >= '\u30A1' and <= '\u30F6')
                continue;
            normalized = value.Normalize(NormalizationForm.FormC);
            if (normalized == value)
                normalized = value;
            break;
        }
        for (int i = 0; i < normalized.Length; i++)
        {
            if (normalized[i] is not (>= '\u30A1' and <= '\u30F6'))
                continue;

            // かな変換が必要な場合だけ、結果の文字列を一度生成します。
            return string.Create(normalized.Length, normalized, static (destination, source) =>
            {
                for (int j = 0; j < source.Length; j++)
                {
                    char c = source[j];
                    destination[j] = c is >= '\u30A1' and <= '\u30F6' ? (char)(c - 0x60) : c;
                }
            });
        }
        return normalized;
    }
}
