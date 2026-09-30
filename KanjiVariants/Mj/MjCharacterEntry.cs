// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using System.Text;

namespace KanjiVariants;

/// <summary>MJ文字情報一覧表の一つの文字図形について、原典の各Unicode表現と分類を保持します。</summary>
/// <param name="MjGlyphName">MJ文字情報一覧表の識別子であるMJ文字図形名です。</param>
/// <param name="CorrespondingUcs">対応するUCSです。実装したUCSとは別の概念です。</param>
/// <param name="ImplementedUcs">実装したUCSです。対応するUCSとは別に保持します。</param>
/// <param name="Ivs">実装したMoji_JohoコレクションIVSの読み取り専用一覧です。原典順で全件を保持し、空欄は共有の空一覧です。基底文字へ自動縮退しません。</param>
/// <param name="Svs">実装したSVSの読み取り専用一覧です。原典順で全件を保持し、空欄は共有の空一覧です。基底文字へ自動縮退しません。</param>
/// <param name="JisX0213">X0213列の原典値です。空欄はnullです。</param>
/// <param name="KanjiPolicy">原典の漢字施策です。空欄はnullです。</param>
/// <param name="CompatibilityIdeograph">対応する互換漢字です。</param>
public sealed record MjCharacterEntry(
    string MjGlyphName,
    Rune? CorrespondingUcs,
    Rune? ImplementedUcs,
    IReadOnlyList<KanjiCharacter> Ivs,
    IReadOnlyList<KanjiCharacter> Svs,
    string? JisX0213,
    MjKanjiPolicy? KanjiPolicy,
    Rune? CompatibilityIdeograph);
