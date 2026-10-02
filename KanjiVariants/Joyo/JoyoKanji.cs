// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文化庁「常用漢字表の音訓索引」の本表字・音訓・語例・備考を調べます。</summary>
public static class JoyoKanji
{
    private static readonly IReadOnlyList<JoyoKanjiEntry> EmptyEntries = Array.Empty<JoyoKanjiEntry>();
    private static readonly IReadOnlyList<KanjiReading> EmptyReadings = Array.Empty<KanjiReading>();
    private static class ReadingKeys
    {
        // 公開する読みは保持し、固定の候補を検索のたびに正規化しないようにします。
        internal static readonly string[][] ByEntry = Build();

        private static string[][] Build()
        {
            var entries = GeneratedJoyoKanjiData.Entries;
            var result = new string[entries.Length][];
            for (int i = 0; i < entries.Length; i++)
            {
                var readings = entries[i].Readings;
                result[i] = new string[readings.Count];
                for (int j = 0; j < readings.Count; j++)
                    result[i][j] = ReadingNormalizer.Normalize(readings[j].Reading);
            }
            return result;
        }
    }
    private static class ReadingIndices
    {
        internal static readonly Dictionary<string, IReadOnlyList<JoyoKanjiEntry>> All = BuildIndex(null);
        internal static readonly Dictionary<string, IReadOnlyList<JoyoKanjiEntry>> On = BuildIndex(KanjiReadingType.On);
        internal static readonly Dictionary<string, IReadOnlyList<JoyoKanjiEntry>> Kun = BuildIndex(KanjiReadingType.Kun);
    }

    /// <summary>Variation Selectorを伴わない本表字かを判定します。</summary>
    public static bool IsJoyo(KanjiCharacter character) => FindIndex(character) >= 0;

    /// <summary>文字列が常用漢字表の本表字かを判定します。無効な文字列は false です。</summary>
    public static bool IsJoyo(string? character) => KanjiCharacter.TryParse(character, out var parsed) && IsJoyo(parsed);

    /// <summary>本表字の情報を取得します。旧字体やIVS/SVSは対象外です。</summary>
    public static JoyoKanjiEntry? Get(KanjiCharacter character)
    {
        int index = FindIndex(character);
        return index < 0 ? null : GeneratedJoyoKanjiData.Entries[index];
    }

    /// <summary>文字列で指定した本表字の情報を取得します。無効な文字列は null です。</summary>
    public static JoyoKanjiEntry? Get(string? character) =>
        KanjiCharacter.TryParse(character, out var parsed) ? Get(parsed) : null;

    /// <summary>本表字の音訓を取得します。対象外なら空の一覧です。</summary>
    public static IReadOnlyList<KanjiReading> GetReadings(KanjiCharacter character) => Get(character)?.Readings ?? EmptyReadings;

    /// <summary>文字列で指定した本表字の音訓を取得します。</summary>
    public static IReadOnlyList<KanjiReading> GetReadings(string? character) => Get(character)?.Readings ?? EmptyReadings;

    /// <summary>指定した音訓があるか、ひらがな・カタカナの差を吸収して完全一致で調べます。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    /// <exception cref="ArgumentException">対象の本表字について、reading が不正なUTF-16で正規化できない場合。</exception>
    public static bool IsReadingSupported(KanjiCharacter character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        int index = FindIndex(character);
        if (index < 0)
            return false;
        string normalized = ReadingNormalizer.Normalize(reading);
        foreach (string key in ReadingKeys.ByEntry[index])
            if (key == normalized)
                return true;
        return false;
    }

    /// <summary>文字列で指定した字の音訓を調べます。無効な字は false です。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    /// <exception cref="ArgumentException">対象の本表字について、reading が不正なUTF-16で正規化できない場合。</exception>
    public static bool IsReadingSupported(string? character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return KanjiCharacter.TryParse(character, out var parsed) && IsReadingSupported(parsed, reading);
    }

    /// <summary>音訓から本表字を完全一致で逆引きします。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    /// <exception cref="ArgumentException">reading が不正なUTF-16で正規化できない場合。</exception>
    public static IReadOnlyList<JoyoKanjiEntry> FindByReading(string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return ReadingIndices.All.TryGetValue(ReadingNormalizer.Normalize(reading), out var entries) ? entries : EmptyEntries;
    }

    /// <summary>音読みまたは訓読みに限定して本表字を逆引きします。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    /// <exception cref="ArgumentException">reading が不正なUTF-16で正規化できない場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException">type が未定義値の場合。</exception>
    public static IReadOnlyList<JoyoKanjiEntry> FindByReading(string reading, KanjiReadingType type)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var index = type switch
        {
            KanjiReadingType.On => ReadingIndices.On,
            KanjiReadingType.Kun => ReadingIndices.Kun,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        return index.TryGetValue(ReadingNormalizer.Normalize(reading), out var entries) ? entries : EmptyEntries;
    }

    private static int FindIndex(KanjiCharacter character) => character.HasVariationSelector
        ? -1
        : Array.BinarySearch(GeneratedJoyoKanjiData.CodePoints, character.BaseCharacter.Value);

    private static Dictionary<string, IReadOnlyList<JoyoKanjiEntry>> BuildIndex(KanjiReadingType? type)
    {
        var map = new Dictionary<string, List<JoyoKanjiEntry>>(StringComparer.Ordinal);
        var entries = GeneratedJoyoKanjiData.Entries;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int j = 0; j < entry.Readings.Count; j++)
            {
                var reading = entry.Readings[j];
                if (type is not null && reading.Type != type)
                    continue;
                string key = ReadingKeys.ByEntry[i][j];
                // 同じ字に同じ読みが複数行あっても、検索結果の字は一度だけ返します。
                if (!seen.Add(key))
                    continue;
                if (!map.TryGetValue(key, out var values))
                    map[key] = values = new List<JoyoKanjiEntry>();
                values.Add(entry);
            }
        }
        var result = new Dictionary<string, IReadOnlyList<JoyoKanjiEntry>>(map.Count, StringComparer.Ordinal);
        foreach (var pair in map)
            result.Add(pair.Key, pair.Value.AsReadOnly());
        return result;
    }

}
