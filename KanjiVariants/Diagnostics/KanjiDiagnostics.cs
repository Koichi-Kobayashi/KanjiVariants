// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>一つの漢字表現について、既存APIのUnicode・文字集合・漢字表・MJ情報をまとめます。</summary>
/// <remarks>登録済みIVS/SVSを基底文字へ縮退せず、各APIと同じ表現を診断します。</remarks>
public static class KanjiDiagnostics
{
    /// <summary>解析済みの漢字表現を、そのまま診断します。</summary>
    /// <param name="character">診断する漢字一文字または登録済みIVS/SVS。</param>
    /// <returns>既存APIの結果を集約した診断結果。</returns>
    /// <remarks>再解析や代替文字への変換を行いません。読み取り専用一覧は既存APIの結果を共有します。</remarks>
    public static KanjiDiagnosticResult Analyze(KanjiCharacter character)
    {
        // 取得済みの情報から所属を求め、同じ表を重複して検索しません。
        var joyo = JoyoKanji.Get(character);
        var grade = EducationKanji.GetGrade(character);
        bool jinmeiyo = JinmeiyoKanji.IsJinmeiyoKanji(character);
        return new KanjiDiagnosticResult
        {
            Character = character,
            BaseCodePoint = character.BaseCharacter.Value,
            IsBaseCharacterBmp = character.BaseCharacter.Value <= 0xFFFF,
            HasVariationSelector = character.HasVariationSelector,
            VariationSelectorCodePoint = character.VariationSelector?.Value,
            UnicodeScalarCount = character.HasVariationSelector ? 2 : 1,
            // Runeの符号単位数を合計し、長さを求めるだけの文字列生成を避けます。
            Utf16CodeUnitCount = character.BaseCharacter.Utf16SequenceLength +
                (character.VariationSelector?.Utf16SequenceLength ?? 0),
            IsJisX0208 = Kanji.IsSupported(character, CharacterSet.JisX0208),
            IsJisX0212 = Kanji.IsSupported(character, CharacterSet.JisX0212),
            IsJisX0213Plane1 = Kanji.IsSupported(character, CharacterSet.JisX0213Plane1),
            IsJisX0213Plane2 = Kanji.IsSupported(character, CharacterSet.JisX0213Plane2),
            IsJoyo = joyo is not null,
            JoyoEntry = joyo,
            IsEducationKanji = grade is not null,
            EducationGrade = grade,
            IsJinmeiyoKanji = jinmeiyo,
            IsNameUsableKanji = joyo is not null || jinmeiyo,
            ReadingEducation = JoyoKanjiEducation.GetReadings(character),
            MjMatches = MjCharacter.Find(character),
        };
    }

    /// <summary>漢字一文字または登録済みIVS/SVSを厳密に解析し、診断します。</summary>
    /// <param name="character">診断する一つの漢字表現。</param>
    /// <returns>解析した漢字表現の診断結果。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="character"/>がnullの場合。</exception>
    /// <exception cref="FormatException">空文字、複数文字、不正UTF-16、漢字以外、未登録Variation Sequenceの場合。</exception>
    /// <remarks>入力制約・例外は<see cref="KanjiCharacter.Parse(string)"/>と同じです。例外を変換しません。</remarks>
    public static KanjiDiagnosticResult Analyze(string character) => Analyze(KanjiCharacter.Parse(character));
}
