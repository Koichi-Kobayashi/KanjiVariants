// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Buffers;
using System.Text;

namespace KanjiVariants;

/// <summary>文字列中の漢字を置換し、結果の文字集合への所属を調べます。</summary>
public static class KanjiText
{
    /// <summary>一意な変換表に変換先がある漢字を置換し、それ以外の文字列部分は維持します。</summary>
    /// <param name="text">置換する文字列。ASCII はそのまま通過します。</param>
    /// <param name="characterSet">変換先として使用できる文字集合。</param>
    /// <returns>置換後の文字列。置換がなければ入力と同じ string インスタンスです。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> が null の場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static string Replace(string text, CharacterSet characterSet)
    {
        ArgumentNullException.ThrowIfNull(text);
        Lookup.Validate(characterSet);
        // 置換が見つかるまでは入力文字列をそのまま使い、不要な割り当てを避けます。
        StringBuilder? builder = null;
        int offset = 0;
        while (offset < text.Length)
        {
            int start = offset;
            if (!TryReadUnit(text, ref offset, out int baseCp, out int? selectorCp))
            {
                builder?.Append(text, start, offset - start);
                continue;
            }
            if (baseCp < 0x80 && selectorCp is null)
            {
                builder?.Append(text, start, offset - start);
                continue;
            }
            int entry = Lookup.FindEntry(baseCp, selectorCp);
            int target = entry < 0 ? 0 : GeneratedData.UniqueAlternatives[entry];
            if (target != 0 && Lookup.IsSupported(target, characterSet) &&
                (selectorCp is not null || target != baseCp))
            {
                // 最初の置換位置までを一度だけコピーし、以降の文字を順に追記します。
                builder ??= new StringBuilder(text.Length).Append(text, 0, start);
                builder.Append(new Rune(target).ToString());
            }
            else
                builder?.Append(text, start, offset - start);
        }
        return builder?.ToString() ?? text;
    }

    /// <summary>文字列の全ての文字が指定文字集合で使用可能かを判定します。</summary>
    /// <param name="text">検証する文字列。ASCII は使用可能として扱います。</param>
    /// <param name="characterSet">所属を確認する文字集合。</param>
    /// <returns>全ての文字が使用可能なら true。未登録の IVS/SVS や不正な UTF-16 があれば false。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> が null の場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/> が未定義の値の場合。</exception>
    public static bool IsSupported(string text, CharacterSet characterSet)
    {
        ArgumentNullException.ThrowIfNull(text);
        Lookup.Validate(characterSet);
        int offset = 0;
        while (offset < text.Length)
        {
            if (!TryReadUnit(text, ref offset, out int baseCp, out int? selectorCp))
                return false;
            if (selectorCp is not null || (baseCp >= 0x80 && !Lookup.IsSupported(baseCp, characterSet)))
                return false;
        }
        return true;
    }

    private static bool TryReadUnit(string text, ref int offset, out int baseCp, out int? selectorCp)
    {
        selectorCp = null;
        var status = Rune.DecodeFromUtf16(text.AsSpan(offset), out var rune, out int consumed);
        if (status != OperationStatus.Done)
        {
            // 不正な UTF-16 は一コードユニットだけ進め、呼び出し元が元の内容を保てるようにします。
            baseCp = 0;
            offset++;
            return false;
        }
        baseCp = rune.Value;
        offset += consumed;
        // 直後の VS も同じ単位に含め、未登録の組み合わせを基底文字だけで置換しないようにします。
        if (offset < text.Length && Rune.DecodeFromUtf16(text.AsSpan(offset), out var next, out int nextLength) == OperationStatus.Done &&
            Lookup.IsVariationSelector(next.Value))
        {
            selectorCp = next.Value;
            offset += nextLength;
        }
        return true;
    }
}
