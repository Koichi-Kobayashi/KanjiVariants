// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文化庁の表記に従った一つの音訓と、その語例・備考。</summary>
/// <remarks>備考欄は字単位のため、同じ字の各音訓に原文全体を保持します。</remarks>
public sealed record KanjiReading(
    KanjiReadingType Type,
    string Reading,
    IReadOnlyList<string> Examples,
    string? Note);
