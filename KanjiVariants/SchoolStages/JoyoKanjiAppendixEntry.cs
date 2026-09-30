// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

namespace KanjiVariants;

/// <summary>文部科学省資料の付表に載る語と読みの割り振りです。</summary>
/// <param name="Words">同じ読みと段階を共有する、原典掲載の語表記。異表記を含みます。</param>
/// <param name="Reading">原典に記載された読み。</param>
/// <param name="Stage">指導段階の目安。</param>
/// <param name="Kind">付表1または付表2の区分。</param>
/// <param name="Note">括弧書きなどの補足。ない場合は null。</param>
public sealed record JoyoKanjiAppendixEntry(
    IReadOnlyList<string> Words,
    string Reading,
    SchoolStage Stage,
    JoyoKanjiAppendixKind Kind,
    string? Note);
