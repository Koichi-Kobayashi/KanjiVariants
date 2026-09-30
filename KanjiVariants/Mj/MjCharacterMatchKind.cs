// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>入力されたUnicode表現とMJ文字情報が一致した理由です。複数の理由はビットORで表します。</summary>
[Flags]
public enum MjCharacterMatchKind
{
    /// <summary>一致なしです。</summary>
    None = 0,
    /// <summary>実装したUCSと一致しました。</summary>
    ImplementedUcs = 1 << 0,
    /// <summary>対応するUCSと一致しました。</summary>
    CorrespondingUcs = 1 << 1,
    /// <summary>実装したMoji_JohoコレクションIVSと一致しました。</summary>
    Ivs = 1 << 2,
    /// <summary>実装したSVSと一致しました。</summary>
    Svs = 1 << 3,
    /// <summary>対応する互換漢字と一致しました。</summary>
    CompatibilityIdeograph = 1 << 4
}
