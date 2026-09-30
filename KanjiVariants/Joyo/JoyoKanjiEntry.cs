// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>常用漢字表の本表に載る一字の情報。</summary>
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
