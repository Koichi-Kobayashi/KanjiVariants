// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants;

/// <summary>文化庁「常用漢字表の音訓索引」の本表字・音訓・語例・備考を調べます。</summary>
public static class JoyoKanji
{
    private static readonly IReadOnlyList<JoyoKanjiEntry> EmptyEntries = Array.Empty<JoyoKanjiEntry>();
    private static readonly IReadOnlyList<KanjiReading> EmptyReadings = Array.Empty<KanjiReading>();
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
    public static bool IsReadingSupported(KanjiCharacter character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var entry = Get(character);
        if (entry is null)
            return false;
        string normalized = Normalize(reading);
        foreach (var item in entry.Readings)
            if (Normalize(item.Reading) == normalized)
                return true;
        return false;
    }

    /// <summary>文字列で指定した字の音訓を調べます。無効な字は false です。</summary>
    public static bool IsReadingSupported(string? character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return KanjiCharacter.TryParse(character, out var parsed) && IsReadingSupported(parsed, reading);
    }

    /// <summary>音訓から本表字を完全一致で逆引きします。</summary>
    public static IReadOnlyList<JoyoKanjiEntry> FindByReading(string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return ReadingIndices.All.TryGetValue(Normalize(reading), out var entries) ? entries : EmptyEntries;
    }

    /// <summary>音読みまたは訓読みに限定して本表字を逆引きします。</summary>
    public static IReadOnlyList<JoyoKanjiEntry> FindByReading(string reading, KanjiReadingType type)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var index = type switch
        {
            KanjiReadingType.On => ReadingIndices.On,
            KanjiReadingType.Kun => ReadingIndices.Kun,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        return index.TryGetValue(Normalize(reading), out var entries) ? entries : EmptyEntries;
    }

    private static int FindIndex(KanjiCharacter character) => character.HasVariationSelector
        ? -1
        : Array.BinarySearch(GeneratedJoyoKanjiData.CodePoints, character.BaseCharacter.Value);

    private static Dictionary<string, IReadOnlyList<JoyoKanjiEntry>> BuildIndex(KanjiReadingType? type)
    {
        var map = new Dictionary<string, List<JoyoKanjiEntry>>(StringComparer.Ordinal);
        foreach (var entry in GeneratedJoyoKanjiData.Entries)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var reading in entry.Readings)
            {
                if (type is not null && reading.Type != type)
                    continue;
                string key = Normalize(reading.Reading);
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

    private static string Normalize(string reading)
    {
        // 正本のカタカナを保持しつつ、検索キーだけひらがなへ揃えます。
        var builder = new StringBuilder(reading.Length);
        foreach (char c in reading.Normalize(NormalizationForm.FormC))
            builder.Append(c is >= '\u30A1' and <= '\u30F6' ? (char)(c - 0x60) : c);
        return builder.ToString();
    }
}
