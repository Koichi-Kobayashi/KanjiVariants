// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>登録済み IVS/SVS の代替文字を決めるときの追加動作。</summary>
[Flags]
public enum KanjiFallbackOptions
{
    /// <summary>MJ縮退マップに定義された候補と一意な変換先だけを使用します。</summary>
    None = 0,

    /// <summary>登録済み IVS/SVS の字形指定を失うことを許可し、基底文字へのフォールバックを有効にします。</summary>
    AllowVariationSelectorFallback = 1 << 0
}
