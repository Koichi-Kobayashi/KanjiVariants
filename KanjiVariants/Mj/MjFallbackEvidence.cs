// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>原典にある縮退方向 Source → Target と、その根拠です。検索はこの関係を逆引きします。</summary>
/// <param name="Source">MJ文字の元表現。候補と同じ表現です。</param>
/// <param name="Target">原典の縮退先。検索した表現です。</param>
/// <param name="MjGlyphName">元のMJ文字図形名。</param>
/// <param name="RelationKind">原典の五分類。</param>
/// <param name="SourceRepresentationKind">MJ列から元表現を採用した理由。対応UCSは実装UCS等がない場合の補助表現です。</param>
/// <param name="JisX0213">原典の縮退先の面区点。</param>
/// <param name="NotificationType">原典の通知種別。記録なしはnull。</param>
/// <param name="HopCount">原典のホップ数。推移検索は実施しません。記録なしはnull。</param>
/// <param name="Note">原典の付記。記録なしはnull。</param>
/// <param name="Table">原典の表。記録なしはnull。</param>
/// <param name="Rank">原典の順位。記録なしはnull。</param>
public sealed record MjFallbackEvidence(
    KanjiCharacter Source, KanjiCharacter Target, string MjGlyphName,
    MjFallbackRelationKind RelationKind, MjCharacterMatchKind SourceRepresentationKind,
    string? JisX0213, string? NotificationType, int? HopCount, string? Note, string? Table, string? Rank);
