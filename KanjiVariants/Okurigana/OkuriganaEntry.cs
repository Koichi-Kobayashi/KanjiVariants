// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文化庁「送り仮名の付け方」の公式掲載語です。未掲載語への規則適用は行いません。</summary>
/// <param name="Word">掲載語の表記。読みや構成関係を示す括弧は注記に分離します。</param>
/// <param name="RuleNumber">掲載箇所の通則番号（1～7）。付表の場合は null。</param>
/// <param name="Kind">本則・例外・許容・付表の区分。</param>
/// <param name="AlternativeForms">同じ項目に示された許容表記。共有の読み取り専用一覧です。</param>
/// <param name="Note">原典の説明、条件、読み、構成関係などの注記。ない場合は null。</param>
public sealed record OkuriganaEntry(
    string Word,
    int? RuleNumber,
    OkuriganaRuleKind Kind,
    IReadOnlyList<string> AlternativeForms,
    string? Note);
