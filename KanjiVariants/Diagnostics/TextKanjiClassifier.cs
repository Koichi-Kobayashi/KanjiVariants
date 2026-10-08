// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

// 文章診断の漢字分類を、既存APIが解析できる漢字表現の範囲から分離します。
internal static class TextKanjiClassifier
{
    // 既存の分類を基に、Unicode 18.0.0で割り当て済みの拡張Jを追加します。
    // 拡張DはU+2B81Eまで割当済みで、直後のU+2B81Fは文章診断から除外します。
    // 解析用Lookupの広い範囲は、既存APIとの互換性のため変更しません。
    // U+3347A～U+3347Fはブロック内でも未割当のため追加しません。
    // 根拠: https://www.unicode.org/versions/Unicode18.0.0/core-spec/chapter-4/ (Table 4-8)
    internal static bool IsKanji(int codePoint) =>
        codePoint is not (0x3005 or 0x3006 or 0x303B or 0x2B81F) &&
        (Lookup.IsRecognizedKanji(codePoint) || codePoint is >= 0x323B0 and <= 0x33479);
}
