// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>指定文字を縮退先とする元表現と全根拠。検索結果は変更できません。</summary>
public sealed class MjFallbackSource
{
    /// <summary>縮退元の表現。IVS/SVSを含む場合があります。</summary>
    public KanjiCharacter Character { get; }

    /// <summary>元表現から検索文字への直接関係の根拠。複数のMJ文字・分類を保持します。</summary>
    public IReadOnlyList<MjFallbackEvidence> Evidence { get; }

    internal MjFallbackSource(KanjiCharacter character, MjFallbackEvidence[] evidence)
    {
        Character = character;
        Evidence = Array.AsReadOnly(evidence);
    }
}
