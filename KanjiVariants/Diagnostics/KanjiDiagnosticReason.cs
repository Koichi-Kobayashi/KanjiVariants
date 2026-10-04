// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文字列中の元の論理単位が使用可能か、実際の置換経路または置換できない理由。</summary>
public enum KanjiDiagnosticReason
{
    /// <summary>元の単位がそのまま使用可能です。置換先はありません。</summary>
    Supported = 0,
    /// <summary>MJ一意変換先が対象集合で使用可能で、Replaceがその変換先を採用します。</summary>
    MjUniqueMappingAvailable = 1,
    /// <summary>MJ一意変換が採用されず、登録済みVSの基底文字へフォールバックします。</summary>
    VariationSelectorFallbackAvailable = 2,
    /// <summary>使用不可で、今回の条件で置換先がありません。単独のVSも含みます。</summary>
    NoReplacement = 3,
    /// <summary>MJ一意変換先は存在しますが対象集合外で、VSフォールバックも成立しません。候補一覧全体の判定ではありません。</summary>
    UniqueAlternativeNotInTargetCharacterSet = 4,
    /// <summary>基底文字とVSをまとめた単位が登録済みIVS/SVSではありません。単独のVSには使いません。</summary>
    UnregisteredVariationSequence = 5,
    /// <summary>不正UTF-16の一符号単位です。置換先はありません。</summary>
    InvalidUtf16 = 6
}
