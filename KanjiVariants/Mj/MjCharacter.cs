// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Globalization;
using System.Text;

namespace KanjiVariants;

/// <summary>MJ文字情報一覧表 Ver.006.02の文字図形名とUnicode表現を検索します。</summary>
public static class MjCharacter
{
    private static readonly IReadOnlyList<MjCharacterMatch> Empty = Array.Empty<MjCharacterMatch>();
    private static readonly IReadOnlyList<KanjiCharacter> EmptySequences = Array.Empty<KanjiCharacter>();

    // 初回利用時に一度だけ構築します。名前・各Unicode表現から同じEntryを共有します。
    private static class Data
    {
        internal static readonly Dictionary<string, MjCharacterEntry> Names = new(StringComparer.Ordinal);
        internal static readonly Dictionary<ulong, IReadOnlyList<MjCharacterMatch>> Matches = Build();

        private static Dictionary<ulong, IReadOnlyList<MjCharacterMatch>> Build()
        {
            var index = new Dictionary<ulong, Dictionary<string, MjCharacterMatch>>();
            foreach (string part in GeneratedMjCharacterData.Parts)
            foreach (string row in part.Split('\n'))
            {
                string[] fields = row.Split('\t');
                var entry = new MjCharacterEntry(fields[0], Scalar(fields[1]), Scalar(fields[2]),
                    Sequences(fields[3]), Sequences(fields[4]), fields[5].Length == 0 ? null : fields[5],
                    fields[6].Length == 0 ? null : Enum.Parse<MjKanjiPolicy>(fields[6]), Scalar(fields[7]));
                Names.Add(entry.MjGlyphName, entry);
                AddScalar(entry.CorrespondingUcs, MjCharacterMatchKind.CorrespondingUcs);
                AddScalar(entry.ImplementedUcs, MjCharacterMatchKind.ImplementedUcs);
                AddSequences(entry.Ivs, MjCharacterMatchKind.Ivs);
                AddSequences(entry.Svs, MjCharacterMatchKind.Svs);
                AddScalar(entry.CompatibilityIdeograph, MjCharacterMatchKind.CompatibilityIdeograph);

                void AddScalar(Rune? value, MjCharacterMatchKind kind)
                {
                    if (value is Rune rune) Add(Lookup.CreateKey(rune.Value, 0), kind);
                }
                void AddSequences(IReadOnlyList<KanjiCharacter> values, MjCharacterMatchKind kind)
                {
                    foreach (var character in values)
                        Add(Lookup.CreateKey(character.BaseCharacter.Value, character.VariationSelector!.Value.Value), kind);
                }
                void Add(ulong key, MjCharacterMatchKind kind)
                {
                    if (!index.TryGetValue(key, out var entries))
                        index.Add(key, entries = new Dictionary<string, MjCharacterMatch>(StringComparer.Ordinal));
                    // 対応UCSと実装UCS等が同じでも、Entryを増やさず一致理由を統合します。
                    if (entries.TryGetValue(entry.MjGlyphName, out var previous)) kind |= previous.MatchKind;
                    entries[entry.MjGlyphName] = new MjCharacterMatch(entry, kind);
                }
            }
            return index.ToDictionary(pair => pair.Key,
                pair => (IReadOnlyList<MjCharacterMatch>)Array.AsReadOnly(pair.Value.Values.ToArray()));
        }

        private static Rune? Scalar(string value) => value.Length == 0 ? null :
            new Rune(int.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

        internal static IReadOnlyList<KanjiCharacter> Sequences(string value)
        {
            if (value.Length == 0) return EmptySequences;
            // IVS・SVSともに同じ経路で複数値を保持し、原典の掲載順も維持します。
            return Array.AsReadOnly(Array.ConvertAll(value.Split(';'), sequence =>
            {
                var parts = sequence.Split('_');
                return KanjiCharacter.Parse(
                    new Rune(int.Parse(parts[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString() +
                    new Rune(int.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());
            }));
        }
    }

    /// <summary>MJ文字図形名の完全一致で一意に取得します。該当なしはnullです。</summary>
    /// <exception cref="ArgumentNullException">mjGlyphNameがnullです。</exception>
    public static MjCharacterEntry? GetByMjGlyphName(string mjGlyphName)
    {
        ArgumentNullException.ThrowIfNull(mjGlyphName);
        // Matchesの初期化を先に完了させ、空のNamesを参照しないようにします。
        _ = Data.Matches;
        return Data.Names.TryGetValue(mjGlyphName, out var entry) ? entry : null;
    }

    /// <summary>Unicode表現に関連するMJ文字情報と一致理由を、共有の読み取り専用一覧で返します。複数件の場合があります。IVS/SVSは自動縮退しません。</summary>
    public static IReadOnlyList<MjCharacterMatch> Find(KanjiCharacter character) =>
        FindKey(Lookup.CreateKey(character.BaseCharacter.Value, character.VariationSelector?.Value ?? 0));

    /// <summary>Unicodeスカラー一文字または登録済みIVS/SVSを検索します。複数件の場合があります。無効な入力は空の一覧で、IVS/SVSは自動縮退しません。</summary>
    /// <exception cref="ArgumentNullException">characterがnullです。</exception>
    public static IReadOnlyList<MjCharacterMatch> Find(string character)
    {
        ArgumentNullException.ThrowIfNull(character);
        // MJには々・〆・〻も含まれるため、一文字のスカラーは漢字の範囲に制限しません。
        if (Rune.DecodeFromUtf16(character.AsSpan(), out var rune, out int consumed) == System.Buffers.OperationStatus.Done &&
            consumed == character.Length)
            return FindKey(Lookup.CreateKey(rune.Value, 0));
        return KanjiCharacter.TryParse(character, out var parsed) ? Find(parsed) : Empty;
    }

    private static IReadOnlyList<MjCharacterMatch> FindKey(ulong key) =>
        Data.Matches.TryGetValue(key, out var matches) ? matches : Empty;
}
