// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>MJ縮退マップの原典に記録された直接関係の分類。一般的な異体字・交換可能性の保証ではありません。</summary>
public enum MjFallbackRelationKind
{
    /// <summary>JIS包摂規準・UCS統合規則。</summary>
    JisInclusionAndUcsUnification = 0,
    /// <summary>法務省戸籍法関連通達・通知。</summary>
    FamilyRegisterNotification = 1,
    /// <summary>法務省告示582号別表第四。</summary>
    MinistryOfJusticeNotice582 = 2,
    /// <summary>辞書類等による関連字。</summary>
    DictionaryRelatedCharacter = 3,
    /// <summary>読み・字形による類推。</summary>
    ReadingAndShapeInference = 4
}
