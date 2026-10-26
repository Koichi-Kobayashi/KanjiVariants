// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Globalization;

namespace KanjiVariants;

/// <summary>MJ縮退マップの直接関係を逆引きします。一般的な異体字検索や自動置換ではありません。</summary>
public static class MjFallback
{
    private static readonly IReadOnlyList<MjFallbackSource> Empty = Array.Empty<MjFallbackSource>();

    // 未使用の候補オブジェクトは生成しません。検索済みの不変結果だけを共有します。
    private static class Cache
    {
        internal static readonly IReadOnlyList<MjFallbackSource>?[] Results = new IReadOnlyList<MjFallbackSource>?[GeneratedMjFallbackData.Targets.Length];
        internal static readonly object Gate = new();
        static Cache() { }
    }

    /// <summary>targetを縮退先に持つ元表現を取得します。自己候補を除き、基底文字・VSの順で返します。</summary>
    /// <remarks>正規化、集合への絞り込み、推移展開はしません。原典の縮退先はVSなしなのでVS付き入力は空です。
    /// 結果と根拠は読み取り専用です。原典に関係がなければ空です。MjCharacterの索引には依存しません。</remarks>
    /// <param name="target">検索する縮退先の漢字表現。</param>
    /// <returns>重複表現を統合した元表現と根拠。</returns>
    public static IReadOnlyList<MjFallbackSource> GetSources(KanjiCharacter target)
    {
        if (target.HasVariationSelector || target.BaseCharacter.Value == 0) return Empty;
        int index = Array.BinarySearch(GeneratedMjFallbackData.Targets, target.BaseCharacter.Value);
        if (index < 0) return Empty;
        var cached = Volatile.Read(ref Cache.Results[index]);
        if (cached is not null) return cached;
        lock (Cache.Gate)
        {
            cached = Cache.Results[index];
            if (cached is not null) return cached;
            cached = Build(target, index);
            Volatile.Write(ref Cache.Results[index], cached);
            return cached;
        }
    }

    private static IReadOnlyList<MjFallbackSource> Build(KanjiCharacter target, int index)
    {
        int start = GeneratedMjFallbackData.Starts[index];
        int end = GeneratedMjFallbackData.Starts[index + 1];
        var result = new MjFallbackSource[end - start];
        for (int i = start; i < end; i++)
        {
            var source = FromKey(GeneratedMjFallbackData.Sources[i]);
            int first = GeneratedMjFallbackData.EvidenceStarts[i];
            int last = GeneratedMjFallbackData.EvidenceStarts[i + 1];
            var evidence = new MjFallbackEvidence[last - first];
            for (int j = first; j < last; j++)
            {
                int offset = GeneratedMjFallbackData.EvidenceReferences[j] * 9;
                var data = GeneratedMjFallbackData.EvidenceData;
                string? Field(int field)
                {
                    int id = data[offset + field];
                    int begin = GeneratedMjFallbackData.TextStarts[id];
                    int length = GeneratedMjFallbackData.TextStarts[id + 1] - begin;
                    return length == 0 ? null : GeneratedMjFallbackData.Text.Substring(begin, length);
                }
                evidence[j - first] = new MjFallbackEvidence(source, target,
                    "MJ" + data[offset].ToString("D6", CultureInfo.InvariantCulture),
                    (MjFallbackRelationKind)data[offset + 1], (MjCharacterMatchKind)data[offset + 2],
                    Field(3), Field(4), data[offset + 5] < 0 ? null : data[offset + 5], Field(6), Field(7), Field(8));
            }
            result[i - start] = new MjFallbackSource(source, evidence);
        }
        return Array.AsReadOnly(result);
    }

    /// <summary>漢字一文字または登録済みIVS/SVSを解析し、縮退元を逆引きします。文字列は正規化しません。</summary>
    /// <param name="target">検索する一つの漢字表現。</param>
    /// <returns>縮退元の一覧。該当なしは空です。</returns>
    /// <exception cref="ArgumentNullException">targetがnullです。</exception>
    /// <exception cref="FormatException">空文字、複数文字、不正UTF-16、未登録VS等です。</exception>
    public static IReadOnlyList<MjFallbackSource> GetSources(string target) => GetSources(KanjiCharacter.Parse(target));

    private static KanjiCharacter FromKey(ulong key)
    {
        int cp = (int)(key & 0x1FFFFF);
        int selector = (int)(key >> 21);
        if (selector == 0) return KanjiCharacter.FromScalar(cp);
        return KanjiCharacter.FromRegisteredSequence(cp, selector);
    }
}
