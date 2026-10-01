// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>一つの漢字表現に関する既存APIの診断結果。</summary>
/// <remarks>IVS/SVSを自動縮退しません。各項目は元の表現についての結果です。</remarks>
public sealed record KanjiDiagnosticResult
{
    /// <summary>診断した元の漢字表現。基底文字への縮退を行いません。</summary>
    public KanjiCharacter Character { get; init; }

    /// <summary>基底文字のUnicodeコードポイント。</summary>
    public int BaseCodePoint { get; init; }

    /// <summary>基底文字がBMP（U+0000～U+FFFF）内かどうか。Variation Selectorの範囲とは独立です。</summary>
    public bool IsBaseCharacterBmp { get; init; }

    /// <summary>登録済みのVariation Selectorを伴う表現かどうか。</summary>
    public bool HasVariationSelector { get; init; }

    /// <summary>Variation SelectorのUnicodeコードポイント。ない場合はnullです。</summary>
    public int? VariationSelectorCodePoint { get; init; }

    /// <summary>表現を構成するUnicodeスカラー数。通常の漢字は1、IVS/SVSは2です。</summary>
    public int UnicodeScalarCount { get; init; }

    /// <summary>表現をUTF-16で符号化したコード単位数。Character.ToString().Lengthに相当し、漢字の個数ではありません。</summary>
    public int Utf16CodeUnitCount { get; init; }

    /// <summary>表現そのものがJIS X 0208に所属するかどうか。</summary>
    public bool IsJisX0208 { get; init; }

    /// <summary>表現そのものがJIS X 0212-1990に所属するかどうか。</summary>
    public bool IsJisX0212 { get; init; }

    /// <summary>表現そのものがJIS X 0213第1面に所属するかどうか。</summary>
    public bool IsJisX0213Plane1 { get; init; }

    /// <summary>表現そのものがJIS X 0213第2面に所属するかどうか。</summary>
    public bool IsJisX0213Plane2 { get; init; }

    /// <summary>Variation Selectorを伴わない常用漢字表の本表字かどうか。</summary>
    public bool IsJoyo { get; init; }

    /// <summary>常用漢字表の本表字情報。本表字でなければnullです。</summary>
    public JoyoKanjiEntry? JoyoEntry { get; init; }

    /// <summary>学年別漢字配当表に掲載された文字かどうか。</summary>
    public bool IsEducationKanji { get; init; }

    /// <summary>小学校の配当学年。教育漢字でなければnullです。</summary>
    public KanjiGrade? EducationGrade { get; init; }

    /// <summary>人名用漢字863字そのものに含まれるかどうか。常用漢字を自動包含しません。</summary>
    public bool IsJinmeiyoKanji { get; init; }

    /// <summary>常用漢字表の本表字または人名用漢字かどうか。漢字以外の名前用文字や戸籍での使用可否全般は判定しません。</summary>
    public bool IsNameUsableKanji { get; init; }

    /// <summary>JIS X 0213第1面または第2面に所属するかどうか。</summary>
    public bool IsJisX0213 => IsJisX0213Plane1 || IsJisX0213Plane2;

    /// <summary>常用漢字の全音訓と指導段階。本表字でなければ共有の空一覧です。</summary>
    /// <remarks><see cref="JoyoKanjiEducation.GetReadings(KanjiCharacter)"/>の読み取り専用結果を共有します。</remarks>
    public IReadOnlyList<KanjiReadingEducation> ReadingEducation { get; init; }
        = Array.Empty<KanjiReadingEducation>();

    /// <summary>元のUnicode表現に一致したMJ文字情報と一致理由。</summary>
    /// <remarks><see cref="MjCharacter.Find(KanjiCharacter)"/>の読み取り専用結果を共有し、一致理由のFlagsとIVS/SVSの区別を保持します。</remarks>
    public IReadOnlyList<MjCharacterMatch> MjMatches { get; init; }
        = Array.Empty<MjCharacterMatch>();
}
