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
            {
                var rows = new Segments(part.AsSpan(), '\n');
                while (rows.HasNext)
                {
                    var entry = ParseEntry(rows.Read());
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
            }
            return index.ToDictionary(pair => pair.Key,
                pair => (IReadOnlyList<MjCharacterMatch>)Array.AsReadOnly(pair.Value.Values.ToArray()));
        }

        private static MjCharacterEntry ParseEntry(ReadOnlySpan<char> row)
        {
            var fields = new Segments(row, '\t');
            // 旧処理と同じ列順で検証し、保存する名称とJIS表記だけを文字列化します。
            string name = fields.Read().ToString();
            var corresponding = Scalar(fields.Read());
            var implemented = Scalar(fields.Read());
            var ivs = Sequences(fields.Read());
            var svs = Sequences(fields.Read());
            var jis = fields.Read();
            string? jisText = jis.IsEmpty ? null : jis.ToString();
            var policy = fields.Read();
            MjKanjiPolicy? kanjiPolicy = policy.IsEmpty ? null : Enum.Parse<MjKanjiPolicy>(policy.ToString());
            return new MjCharacterEntry(name, corresponding, implemented, ivs, svs,
                jisText, kanjiPolicy, Scalar(fields.Read()));
        }

        private static Rune? Scalar(ReadOnlySpan<char> value) => value.Length == 0 ? null :
            new Rune(int.Parse(value[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));

        internal static IReadOnlyList<KanjiCharacter> Sequences(string value) =>
            value.Length == 0 ? EmptySequences : Sequences(value.AsSpan());

        private static IReadOnlyList<KanjiCharacter> Sequences(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty) return EmptySequences;
            // 分割用の配列を作らず、公開するシーケンス配列だけを原典の掲載順で構築します。
            int count = 1;
            foreach (char character in value)
                if (character == ';') count++;
            var result = new KanjiCharacter[count];
            var sequences = new Segments(value, ';');
            for (int i = 0; i < result.Length; i++)
                result[i] = ParseSequence(sequences.Read());
            return Array.AsReadOnly(result);
        }

        private static KanjiCharacter ParseSequence(ReadOnlySpan<char> sequence)
        {
            var parts = new Segments(sequence, '_');
            var baseRune = new Rune(int.Parse(parts.Read(), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            var selector = new Rune(int.Parse(parts.Read(), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            // Runeごとの文字列と連結結果を作らず、既存Parseに渡す文字列を一度だけ生成します。
            // 登録済みIVS/SVSの検証は既存APIに委ね、余分な区切り要素も旧処理と同様に無視します。
            Span<char> buffer = stackalloc char[4];
            int length = baseRune.EncodeToUtf16(buffer);
            length += selector.EncodeToUtf16(buffer[length..]);
            return KanjiCharacter.Parse(new string(buffer[..length]));
        }

        // Splitの空要素・末尾要素を維持します。列不足も旧配列参照と同じ例外にします。
        private ref struct Segments
        {
            private ReadOnlySpan<char> remaining;
            private readonly char separator;
            private bool hasNext;

            internal Segments(ReadOnlySpan<char> value, char separator)
            {
                remaining = value;
                this.separator = separator;
                hasNext = true;
            }

            internal bool HasNext => hasNext;

            internal ReadOnlySpan<char> Read()
            {
                if (!hasNext) throw new IndexOutOfRangeException();
                int end = remaining.IndexOf(separator);
                if (end < 0)
                {
                    hasNext = false;
                    return remaining;
                }
                var segment = remaining[..end];
                remaining = remaining[(end + 1)..];
                return segment;
            }
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
