// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants;

/// <summary>文化庁「送り仮名の付け方」の公式掲載語を参照します。未掲載語の正誤判定は行いません。</summary>
public static class Okurigana
{
    private static readonly IReadOnlyList<OkuriganaEntry> Empty = Array.Empty<OkuriganaEntry>();

    private static class Store
    {
        internal static readonly Dictionary<string, IReadOnlyList<OkuriganaEntry>> ByWord;
        internal static readonly IReadOnlyList<OkuriganaEntry>[] ByRule;
        internal static readonly IReadOnlyList<OkuriganaEntry>[] ByKind;

        static Store()
        {
            var words = new Dictionary<string, List<OkuriganaEntry>>(StringComparer.Ordinal);
            var rules = Enumerable.Range(0, 7).Select(_ => new List<OkuriganaEntry>()).ToArray();
            var kinds = Enumerable.Range(0, 4).Select(_ => new List<OkuriganaEntry>()).ToArray();
            foreach (var entry in GeneratedOkuriganaData.Entries)
            {
                if ((uint)entry.Kind > (uint)OkuriganaRuleKind.Appendix ||
                    (entry.Kind == OkuriganaRuleKind.Appendix) != (entry.RuleNumber is null) ||
                    entry.RuleNumber is < 1 or > 7)
                    throw new InvalidOperationException("生成した送り仮名データの区分が不正です。");
                if (entry.RuleNumber is int number)
                    rules[number - 1].Add(entry);
                kinds[(int)entry.Kind].Add(entry);

                // 本体の表記と許容表記の両方から、同じ掲載項目を参照できるようにします。
                var keys = new HashSet<string>(StringComparer.Ordinal)
                {
                    entry.Word.Normalize(NormalizationForm.FormC),
                };
                foreach (var form in entry.AlternativeForms)
                    keys.Add(form.Normalize(NormalizationForm.FormC));
                foreach (var key in keys)
                {
                    if (!words.TryGetValue(key, out var values))
                        words.Add(key, values = new List<OkuriganaEntry>());
                    values.Add(entry);
                }
            }
            ByWord = new Dictionary<string, IReadOnlyList<OkuriganaEntry>>(words.Count, StringComparer.Ordinal);
            foreach (var pair in words)
                ByWord.Add(pair.Key, pair.Value.AsReadOnly());
            ByRule = rules.Select(values => (IReadOnlyList<OkuriganaEntry>)values.AsReadOnly()).ToArray();
            ByKind = kinds.Select(values => (IReadOnlyList<OkuriganaEntry>)values.AsReadOnly()).ToArray();
        }
    }

    /// <summary>公式掲載語または同じ項目の許容表記を、NFCで正規化して完全一致で検索します。該当なしは空一覧です。</summary>
    /// <exception cref="ArgumentNullException">word が null の場合。</exception>
    public static IReadOnlyList<OkuriganaEntry> Find(string word)
    {
        ArgumentNullException.ThrowIfNull(word);
        return Store.ByWord.TryGetValue(word.Normalize(NormalizationForm.FormC), out var values) ? values : Empty;
    }

    /// <summary>通則1～7の公式掲載語を共有の読み取り専用一覧で返します。付表は含めません。</summary>
    /// <exception cref="ArgumentOutOfRangeException">ruleNumber が1～7以外の場合。</exception>
    public static IReadOnlyList<OkuriganaEntry> GetByRule(int ruleNumber)
    {
        if (ruleNumber is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(ruleNumber));
        return Store.ByRule[ruleNumber - 1];
    }

    /// <summary>指定した区分の公式掲載語を共有の読み取り専用一覧で返します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">kind が未定義値の場合。</exception>
    public static IReadOnlyList<OkuriganaEntry> GetByKind(OkuriganaRuleKind kind)
    {
        if ((uint)kind > (uint)OkuriganaRuleKind.Appendix)
            throw new ArgumentOutOfRangeException(nameof(kind));
        return Store.ByKind[(int)kind];
    }
}
