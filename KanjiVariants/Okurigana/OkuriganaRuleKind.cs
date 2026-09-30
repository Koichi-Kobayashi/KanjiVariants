// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文化庁「送り仮名の付け方」の公式掲載語の区分です。未掲載語の正誤を表しません。</summary>
public enum OkuriganaRuleKind
{
    /// <summary>本則。注意中の本則に関する語例も注記付きで含みます。</summary>
    Principle,

    /// <summary>例外。通則6の例外として別立てされた通則7も含みます。</summary>
    Exception,

    /// <summary>許容。本文に示された条件と別表記を参照します。</summary>
    Permitted,

    /// <summary>付表の語。</summary>
    Appendix,
}
