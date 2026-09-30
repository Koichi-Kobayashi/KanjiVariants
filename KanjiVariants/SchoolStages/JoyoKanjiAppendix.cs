// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants;

/// <summary>文部科学省の音訓割り振り表にある付表1・付表2の語を参照します。</summary>
public static class JoyoKanjiAppendix
{
    private static readonly IReadOnlyList<JoyoKanjiAppendixEntry> Empty =
        Array.Empty<JoyoKanjiAppendixEntry>();

    private static class Store
    {
        internal static readonly IReadOnlyList<JoyoKanjiAppendixEntry> All;
        internal static readonly IReadOnlyList<JoyoKanjiAppendixEntry>[] ByStage;
        internal static readonly IReadOnlyList<JoyoKanjiAppendixEntry>[] ByKind;
        internal static readonly Dictionary<string, IReadOnlyList<JoyoKanjiAppendixEntry>> ByWord;
        internal static readonly Dictionary<string, IReadOnlyList<JoyoKanjiAppendixEntry>> ByReading;

        static Store()
        {
            var source = GeneratedJoyoKanjiEducationData.Appendix;
            All = Array.AsReadOnly(source);
            var stageLists = new List<JoyoKanjiAppendixEntry>[] { new(), new(), new() };
            var kindLists = new List<JoyoKanjiAppendixEntry>[] { new(), new() };
            var words = new Dictionary<string, List<JoyoKanjiAppendixEntry>>(StringComparer.Ordinal);
            var readings = new Dictionary<string, List<JoyoKanjiAppendixEntry>>(StringComparer.Ordinal);
            foreach (var entry in source)
            {
                if ((uint)entry.Stage > (uint)SchoolStage.HighSchool ||
                    (uint)entry.Kind > (uint)JoyoKanjiAppendixKind.PrefectureName)
                    throw new InvalidOperationException("生成した付表データの区分が不正です。");
                stageLists[(int)entry.Stage].Add(entry);
                kindLists[(int)entry.Kind].Add(entry);
                foreach (var word in entry.Words)
                    Add(words, word.Normalize(NormalizationForm.FormC), entry);
                Add(readings, JoyoEducationKey.Normalize(entry.Reading), entry);
            }
            ByStage = new IReadOnlyList<JoyoKanjiAppendixEntry>[]
            {
                stageLists[0].AsReadOnly(), stageLists[1].AsReadOnly(), stageLists[2].AsReadOnly(),
            };
            ByKind = new IReadOnlyList<JoyoKanjiAppendixEntry>[]
            {
                kindLists[0].AsReadOnly(), kindLists[1].AsReadOnly(),
            };
            ByWord = Freeze(words);
            ByReading = Freeze(readings);
        }

        private static void Add(Dictionary<string, List<JoyoKanjiAppendixEntry>> index,
            string key, JoyoKanjiAppendixEntry entry)
        {
            if (!index.TryGetValue(key, out var values))
                index.Add(key, values = new List<JoyoKanjiAppendixEntry>());
            values.Add(entry);
        }

        private static Dictionary<string, IReadOnlyList<JoyoKanjiAppendixEntry>> Freeze(
            Dictionary<string, List<JoyoKanjiAppendixEntry>> source)
        {
            var result = new Dictionary<string, IReadOnlyList<JoyoKanjiAppendixEntry>>(
                source.Count, StringComparer.Ordinal);
            foreach (var pair in source)
                result.Add(pair.Key, pair.Value.AsReadOnly());
            return result;
        }
    }

    /// <summary>付表1・付表2の全件を共有の読み取り専用一覧で返します。</summary>
    public static IReadOnlyList<JoyoKanjiAppendixEntry> GetAll() => Store.All;

    /// <summary>指定段階に割り振られた付表の語を返します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">stage が未定義値の場合。</exception>
    public static IReadOnlyList<JoyoKanjiAppendixEntry> GetByStage(SchoolStage stage)
    {
        if ((uint)stage > (uint)SchoolStage.HighSchool)
            throw new ArgumentOutOfRangeException(nameof(stage));
        return Store.ByStage[(int)stage];
    }

    /// <summary>付表1または付表2に限定して返します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">kind が未定義値の場合。</exception>
    public static IReadOnlyList<JoyoKanjiAppendixEntry> GetByKind(JoyoKanjiAppendixKind kind)
    {
        if ((uint)kind > (uint)JoyoKanjiAppendixKind.PrefectureName)
            throw new ArgumentOutOfRangeException(nameof(kind));
        return Store.ByKind[(int)kind];
    }

    /// <summary>語表記をNFCで正規化し、完全一致で付表を検索します。</summary>
    /// <exception cref="ArgumentNullException">word が null の場合。</exception>
    public static IReadOnlyList<JoyoKanjiAppendixEntry> FindByWord(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        return Store.ByWord.TryGetValue(word.Normalize(NormalizationForm.FormC), out var values)
            ? values : Empty;
    }

    /// <summary>読みの仮名種別の差を吸収し、完全一致で付表を検索します。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static IReadOnlyList<JoyoKanjiAppendixEntry> FindByReading(string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return Store.ByReading.TryGetValue(JoyoEducationKey.Normalize(reading), out var values)
            ? values : Empty;
    }
}
