// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>既存の常用漢字音訓と、文部科学省資料における指導段階を結び付けます。</summary>
/// <param name="Reading">既存の <see cref="JoyoKanji"/> が保持する音訓。同じインスタンスを参照します。</param>
/// <param name="Stage">この音訓の指導段階の目安。</param>
/// <param name="IsSpecialOrLimited">原典で1字下げされ、「特別なもの、又は用法のごく狭いもの」とされる音訓なら true。</param>
public sealed record KanjiReadingEducation(
    KanjiReading Reading,
    SchoolStage Stage,
    bool IsSpecialOrLimited);
