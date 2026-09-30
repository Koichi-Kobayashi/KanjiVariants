// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Buffers;
using System.Text;

namespace KanjiVariants;

/// <summary>認識可能な漢字の単一の Unicode 表現。補助平面の漢字と登録済み IVS/SVS を含みます。</summary>
public readonly record struct KanjiCharacter
{
    /// <summary>基底文字。</summary>
    public Rune BaseCharacter { get; }

    /// <summary>登録済みの Variation Selector。ない場合は null。</summary>
    public Rune? VariationSelector { get; }

    /// <summary>Variation Selector があるかどうか。</summary>
    public bool HasVariationSelector => VariationSelector is not null;

    private KanjiCharacter(Rune baseCharacter, Rune? variationSelector)
    {
        BaseCharacter = baseCharacter;
        VariationSelector = variationSelector;
    }

    /// <summary>認識可能な漢字一文字または登録済み IVS/SVS を解析します。</summary>
    /// <param name="value">解析する文字列。</param>
    /// <returns>解析された漢字表現。</returns>
    /// <exception cref="ArgumentNullException">value が null の場合。</exception>
    /// <exception cref="FormatException">認識可能な漢字一文字ではない場合。</exception>
    public static KanjiCharacter Parse(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!TryParse(value, out var character))
            throw new FormatException("認識可能な漢字一文字または登録済み IVS/SVS を指定してください。");
        return character;
    }

    /// <summary>認識可能な漢字一文字または登録済み IVS/SVS を解析します。</summary>
    /// <param name="value">解析する文字列。</param>
    /// <param name="character">成功時は解析結果。失敗時は既定値です。</param>
    /// <returns>入力全体を漢字表現として解析できた場合は true。</returns>
    public static bool TryParse(string? value, out KanjiCharacter character)
    {
        character = default;
        if (string.IsNullOrEmpty(value))
            return false;
        var span = value.AsSpan();
        // char ではなく Rune で読み、補助平面の漢字も一文字として扱います。
        if (Rune.DecodeFromUtf16(span, out var baseRune, out int consumed) != OperationStatus.Done ||
            !Lookup.IsRecognizedKanji(baseRune.Value))
            return false;
        if (consumed == span.Length)
        {
            character = new KanjiCharacter(baseRune, null);
            return true;
        }
        // VS が続く場合は、登録済みの正規な IVS/SVS で文字列全体が構成されるときだけ受け入れます。
        if (Rune.DecodeFromUtf16(span[consumed..], out var selector, out int selectorLength) != OperationStatus.Done ||
            consumed + selectorLength != span.Length ||
            !Lookup.IsVariationSelector(selector.Value) ||
            !Lookup.IsRegisteredVariation(baseRune.Value, selector.Value))
            return false;
        character = new KanjiCharacter(baseRune, selector);
        return true;
    }

    internal static KanjiCharacter FromScalar(int codePoint) => new(new Rune(codePoint), null);

    /// <inheritdoc />
    public override string ToString() => VariationSelector is Rune selector
        ? BaseCharacter.ToString() + selector.ToString()
        : BaseCharacter.ToString();
}
