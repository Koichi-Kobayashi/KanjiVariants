// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>漢字と読みの組み合わせについて、常用漢字表への掲載有無を調べます。読みの正誤は判定しません。</summary>
public static class JoyoReading
{
    /// <summary>本表字と読みの組み合わせが常用漢字表に掲載されているかを、仮名の差を吸収して完全一致で判定します。</summary>
    /// <remarks>旧字体・異体字・IVS/SVSは自動変換しません。対象外の文字や空の読みは false です。</remarks>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    /// <exception cref="ArgumentException">対象の本表字について、reading が不正なUTF-16で正規化できない場合。</exception>
    public static bool IsListedReading(KanjiCharacter character, string reading) =>
        JoyoKanji.IsReadingSupported(character, reading);

    /// <summary>文字列で指定した本表字と読みの組み合わせが、常用漢字表に掲載されているかを判定します。</summary>
    /// <remarks>無効な文字列、旧字体・異体字・IVS/SVS、空の読みは false です。</remarks>
    /// <exception cref="ArgumentNullException">reading が null の場合。character が無効でも例外になります。</exception>
    /// <exception cref="ArgumentException">対象の本表字について、reading が不正なUTF-16で正規化できない場合。</exception>
    public static bool IsListedReading(string? character, string reading) =>
        JoyoKanji.IsReadingSupported(character, reading);

    /// <summary>漢字は常用漢字表の本表字であるが、読みが掲載音訓に含まれないかを判定します。</summary>
    /// <remarks>表外読みは未掲載の読みを示すだけで、その読みが誤りであることを意味しません。
    /// 空の読みも未掲載として扱います。旧字体・異体字・IVS/SVSは自動変換せず false です。</remarks>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    /// <exception cref="ArgumentException">対象の本表字について、reading が不正なUTF-16で正規化できない場合。</exception>
    public static bool IsHyogaiReading(KanjiCharacter character, string reading)
    {
        // 文字が対象外の場合も、読みのnull検証は既存APIと同様に先に行います。
        ArgumentNullException.ThrowIfNull(reading);
        return JoyoKanji.IsJoyo(character) && !IsListedReading(character, reading);
    }

    /// <summary>文字列で指定した漢字が本表字であり、読みが常用漢字表に掲載されていないかを判定します。</summary>
    /// <remarks>表外読みは読みの誤りを意味しません。空の読みは未掲載、無効な文字列やIVS/SVSは対象外です。</remarks>
    /// <exception cref="ArgumentNullException">reading が null の場合。character が無効でも例外になります。</exception>
    /// <exception cref="ArgumentException">対象の本表字について、reading が不正なUTF-16で正規化できない場合。</exception>
    public static bool IsHyogaiReading(string? character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return JoyoKanji.IsJoyo(character) && !IsListedReading(character, reading);
    }
}
