// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>一つの漢字表現の詳細情報と、文字列全体の文字集合適合性・置換判断を診断します。</summary>
/// <remarks>Analyzeは元の漢字表現の情報を集約します。AnalyzeTextは元の文字列を保持し、指定条件での置換先を示します。</remarks>
public static class KanjiDiagnostics
{
    /// <summary>文字列全体を既存のKanjiTextと同じ論理単位・判定規則で診断します。</summary>
    /// <param name="text">元の入力文字列。コピーや正規化を行いません。</param>
    /// <param name="characterSet">所属と置換先を確認する文字集合。</param>
    /// <param name="options">登録済みVSの基底文字へのフォールバック設定。</param>
    /// <returns>置換前の適合性と、実際にReplaceが採用する置換先を含む診断結果。</returns>
    /// <remarks>不正UTF-16は例外ではなく診断項目として返します。CRとLFは別の単位です。候補一覧や各漢字表の詳細情報は取得しません。</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="text"/>がnullの場合。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="characterSet"/>が未定義の場合、または<paramref name="options"/>に未定義bitが含まれる場合。</exception>
    public static KanjiTextDiagnosticResult AnalyzeText(string text, CharacterSet characterSet,
        KanjiFallbackOptions options = KanjiFallbackOptions.None)
    {
        ArgumentNullException.ThrowIfNull(text);
        Lookup.Validate(characterSet);
        Lookup.Validate(options);
        if (text.Length == 0)
            return new KanjiTextDiagnosticResult(text, characterSet, options, true,
                Array.Empty<KanjiTextDiagnosticItem>());

        var items = new List<KanjiTextDiagnosticItem>();
        bool allSupported = true;
        int offset = 0;
        while (offset < text.Length)
        {
            int start = offset;
            bool valid = KanjiText.TryReadUnit(text, ref offset, out int baseCp, out int? selectorCp);
            string original = text.Substring(start, offset - start);
            string? replacement = null;
            KanjiDiagnosticReason reason;
            if (!valid)
                reason = KanjiDiagnosticReason.InvalidUtf16;
            // 既存hot pathの条件式を変えず、診断側から同じAPIを再利用します。
            // Originalとして保持する文字列をそのまま判定にも再利用し、
            // 判定専用の追加文字列は生成しません。
            else if (KanjiText.IsSupported(original, characterSet))
                reason = KanjiDiagnosticReason.Supported;
            else
            {
                int target = Lookup.ResolveAlternative(baseCp, selectorCp, characterSet, options, out int uniqueTarget);
                if (target != 0 && (selectorCp is not null || target != baseCp))
                {
                    replacement = new System.Text.Rune(target).ToString();
                    reason = target == uniqueTarget ? KanjiDiagnosticReason.MjUniqueMappingAvailable
                        : KanjiDiagnosticReason.VariationSelectorFallbackAvailable;
                }
                // 単独のVSは基底文字として分類しません。連続VSでもreaderの区切りを維持します。
                else if (selectorCp is int selector && !Lookup.IsVariationSelector(baseCp) &&
                    !Lookup.IsRegisteredVariation(baseCp, selector))
                    reason = KanjiDiagnosticReason.UnregisteredVariationSequence;
                else if (uniqueTarget != 0)
                    reason = KanjiDiagnosticReason.UniqueAlternativeNotInTargetCharacterSet;
                else
                    reason = KanjiDiagnosticReason.NoReplacement;
            }
            allSupported &= reason == KanjiDiagnosticReason.Supported;
            items.Add(new KanjiTextDiagnosticItem(original, start, replacement, reason));
        }
        return new KanjiTextDiagnosticResult(text, characterSet, options, allSupported, items.AsReadOnly());
    }

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
