// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>Unicode表現から検索したMJ文字情報と、検索一致理由です。</summary>
/// <param name="Entry">一致したMJ文字情報です。</param>
/// <param name="MatchKind">一致理由です。同じEntryの複数理由はビットORでまとめます。</param>
public sealed record MjCharacterMatch(MjCharacterEntry Entry, MjCharacterMatchKind MatchKind);
