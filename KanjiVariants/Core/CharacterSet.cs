// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>代替先として使用する文字集合。</summary>
public enum CharacterSet
{
    /// <summary>JIS X 0208。第1水準および第2水準の漢字を含みます。</summary>
    JisX0208 = 0,

    /// <summary>JIS X 0213 第1面。</summary>
    JisX0213Plane1 = 1,

    /// <summary>JIS X 0213 第2面。</summary>
    JisX0213Plane2 = 2,

    /// <summary>JIS X 0213 第1面および第2面。</summary>
    JisX0213 = 3,

    /// <summary>JIS X 0212-1990。補助漢字と非漢字を含む独立した補助文字集合で、JIS X 0208を自動包含しません。</summary>
    JisX0212 = 4
}
