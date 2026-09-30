// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants;

/// <summary>常用漢字の音訓ごとに、文部科学省資料の指導段階を参照します。</summary>
public static class JoyoKanjiEducation
{
    private static readonly IReadOnlyList<KanjiReadingEducation> Empty = Array.Empty<KanjiReadingEducation>();

    private static class Store
    {
        internal static readonly Dictionary<int, IReadOnlyList<KanjiReadingEducation>> ByCharacter = new();
        internal static readonly Dictionary<(int, string), KanjiReadingEducation> ByReading = new();
        internal static readonly IReadOnlyList<KanjiReadingEducation>[] ByStage;

        static Store()
        {
            var source = GeneratedJoyoKanjiData.Entries;
            var offsets = GeneratedJoyoKanjiEducationData.Offsets;
            var stages = GeneratedJoyoKanjiEducationData.Stages;
            var special = GeneratedJoyoKanjiEducationData.Special;
            if (offsets.Length != source.Length + 1 ||
                stages.Length != special.Length || offsets[source.Length] != stages.Length)
                throw new InvalidOperationException("生成した音訓別学校段階データが常用漢字表と一致しません。");

            var grouped = new List<KanjiReadingEducation>[]
            {
                new(), new(), new(),
            };
            for (int i = 0; i < source.Length; i++)
            {
                var joyo = source[i];
                int start = offsets[i];
                if (offsets[i + 1] - start != joyo.Readings.Count)
                    throw new InvalidOperationException("常用漢字の音訓数が生成済みデータと一致しません。");
                var entries = new KanjiReadingEducation[joyo.Readings.Count];
                int codePoint = joyo.Character.BaseCharacter.Value;
                for (int j = 0; j < entries.Length; j++)
                {
                    int position = start + j;
                    var stage = (SchoolStage)stages[position];
                    if ((uint)stage > (uint)SchoolStage.HighSchool || special[position] > 1)
                        throw new InvalidOperationException("生成済み音訓データの値が不正です。");
                    // KanjiReadingは既存の本表データを参照し、語例や備考を複製しません。
                    var item = new KanjiReadingEducation(joyo.Readings[j], stage, special[position] == 1);
                    entries[j] = item;
                    grouped[(int)stage].Add(item);
                    if (!ByReading.TryAdd((codePoint, JoyoEducationKey.Normalize(item.Reading.Reading)), item))
                        throw new InvalidOperationException("同じ漢字に正規化後の同一音訓が重複しています。");
                }
                ByCharacter.Add(codePoint, Array.AsReadOnly(entries));
            }
            ByStage = new IReadOnlyList<KanjiReadingEducation>[]
            {
                grouped[0].AsReadOnly(), grouped[1].AsReadOnly(), grouped[2].AsReadOnly(),
            };
        }
    }

    /// <summary>本表字の全音訓を、既存の音訓順に返します。対象外なら共有の空一覧です。</summary>
    public static IReadOnlyList<KanjiReadingEducation> GetReadings(KanjiCharacter character) =>
        !character.HasVariationSelector &&
        Store.ByCharacter.TryGetValue(character.BaseCharacter.Value, out var entries) ? entries : Empty;

    /// <summary>文字列で指定した本表字の全音訓を返します。無効な文字列なら共有の空一覧です。</summary>
    public static IReadOnlyList<KanjiReadingEducation> GetReadings(string? character) =>
        KanjiCharacter.TryParse(character, out var parsed) ? GetReadings(parsed) : Empty;

    /// <summary>漢字と正規化後の読みが完全一致する音訓を返します。見つからなければ null です。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static KanjiReadingEducation? GetReading(KanjiCharacter character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return !character.HasVariationSelector &&
               Store.ByReading.TryGetValue((character.BaseCharacter.Value, JoyoEducationKey.Normalize(reading)), out var entry)
            ? entry : null;
    }

    /// <summary>文字列で指定した漢字と読みを検索します。無効な文字列なら null です。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static KanjiReadingEducation? GetReading(string? character, string reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        return KanjiCharacter.TryParse(character, out var parsed) ? GetReading(parsed, reading) : null;
    }

    /// <summary>漢字と読みが一致した場合、その指導段階を返します。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static SchoolStage? GetReadingStage(KanjiCharacter character, string reading) =>
        GetReading(character, reading)?.Stage;

    /// <summary>文字列で指定した漢字と読みの指導段階を返します。</summary>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static SchoolStage? GetReadingStage(string? character, string reading) =>
        GetReading(character, reading)?.Stage;

    /// <summary>指定した音訓の指導段階が stage と一致するか調べます。</summary>
    /// <exception cref="ArgumentOutOfRangeException">stage が未定義値の場合。</exception>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static bool IsReadingAssigned(KanjiCharacter character, string reading, SchoolStage stage)
    {
        ValidateStage(stage);
        return GetReading(character, reading)?.Stage == stage;
    }

    /// <summary>文字列で指定した漢字の音訓の指導段階を調べます。</summary>
    /// <exception cref="ArgumentOutOfRangeException">stage が未定義値の場合。</exception>
    /// <exception cref="ArgumentNullException">reading が null の場合。</exception>
    public static bool IsReadingAssigned(string? character, string reading, SchoolStage stage)
    {
        ValidateStage(stage);
        return GetReading(character, reading)?.Stage == stage;
    }

    /// <summary>指定した指導段階の音訓を、共有の読み取り専用一覧で返します。</summary>
    /// <exception cref="ArgumentOutOfRangeException">stage が未定義値の場合。</exception>
    public static IReadOnlyList<KanjiReadingEducation> GetByStage(SchoolStage stage)
    {
        ValidateStage(stage);
        return Store.ByStage[(int)stage];
    }

    private static void ValidateStage(SchoolStage stage)
    {
        if ((uint)stage > (uint)SchoolStage.HighSchool)
            throw new ArgumentOutOfRangeException(nameof(stage));
    }
}

internal static class JoyoEducationKey
{
    internal static string Normalize(string value)
    {
        // 既存JoyoKanjiの検索と同じく、正本の読みは変えずに検索キーだけ揃えます。
        var builder = new StringBuilder(value.Length);
        foreach (char character in value.Normalize(NormalizationForm.FormC))
            builder.Append(character is >= '\u30A1' and <= '\u30F6'
                ? (char)(character - 0x60) : character);
        return builder.ToString();
    }
}
