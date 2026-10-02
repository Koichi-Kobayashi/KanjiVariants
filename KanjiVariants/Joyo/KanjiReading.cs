// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文化庁の表記に従った一つの音訓と、その語例・備考。</summary>
/// <remarks>備考欄は字単位のため、同じ字の各音訓に原文全体を保持します。</remarks>
/// <param name="Type">音読みまたは訓読みの区分。</param>
/// <param name="Reading">文化庁の原典に掲載された読み。</param>
/// <param name="Examples">原典に掲載された語例の読み取り専用一覧。</param>
/// <param name="Note">字単位の備考全文。備考がない場合は null です。</param>
public sealed record KanjiReading(
    KanjiReadingType Type,
    string Reading,
    IReadOnlyList<string> Examples,
    string? Note);
