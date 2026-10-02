// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>常用漢字表の本表に載る一字の情報。</summary>
/// <param name="Character">常用漢字表の本表字。</param>
/// <param name="OldForm">原典に併記された旧字体等が一つの場合の文字。ない場合や複数ある場合は null です。全件は <see cref="OldForms"/> を参照してください。</param>
/// <param name="Readings">原典の音訓順に並ぶ読み取り専用の音訓一覧。</param>
public sealed record JoyoKanjiEntry(
    KanjiCharacter Character,
    KanjiCharacter? OldForm,
    IReadOnlyList<KanjiReading> Readings)
{
    /// <summary>原典に併記された旧字体等の全件。単数のときは <see cref="OldForm"/> と一致します。</summary>
    /// <remarks>「弁」のように複数ある場合、<see cref="OldForm"/> は null です。</remarks>
    public IReadOnlyList<KanjiCharacter> OldForms { get; init; } = Array.Empty<KanjiCharacter>();

    /// <summary>角括弧の字形注記を含む原典の表記。</summary>
    public string SourceLabel { get; init; } = string.Empty;
}
