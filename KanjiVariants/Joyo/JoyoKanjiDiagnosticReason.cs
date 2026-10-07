// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>一論理単位が常用漢字表の本表字かどうかの診断理由。</summary>
public enum JoyoKanjiDiagnosticReason
{
    /// <summary>漢字以外。常用漢字違反としては扱いません。</summary>
    NotKanji = 0,
    /// <summary>Variation Selectorを伴わない常用漢字表の本表字。</summary>
    JoyoKanji = 1,
    /// <summary>本表字以外の漢字、または登録有無によらず漢字のVS付き表現。</summary>
    NotJoyoKanji = 2,
    /// <summary>不正なUTF-16コード単位。非常用漢字の一覧には含めません。</summary>
    InvalidUtf16 = 3,
}
