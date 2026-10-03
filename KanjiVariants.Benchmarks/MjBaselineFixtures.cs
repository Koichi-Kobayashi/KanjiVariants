// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;

internal static class MjBaselineFixtures
{
    // 現行生成データ58,862行の先頭・中央(0始まり29,431行目)・末尾。
    internal const string NameStart = "MJ000001";
    internal const string NameMiddle = "MJ030613";
    internal const string NameEnd = "MJ068101";
    internal const string NameMiss = "MJ999999";

    // 既存MJテストと生成データで確認した固定入力。検索はstring overloadで統一します。
    internal const string Bmp = "\u9AD9";
    internal const string Supplementary = "\U00020BB7";
    internal const string Implemented = "\uF91D";
    internal const string Corresponding = "\u3404";
    internal const string Ivs = "\u8FBB\U000E0102";
    internal const string Svs = "\u6B04\uFE00";
    // U+3404にはMJ000007/000008が対応。Correspondingと同じ入力を意図的に使用します。
    internal const string Multiple = Corresponding;
    // 有効なscalarで索引検索まで進むmiss。無効文字列では初期化を測れません。
    internal const string Miss = "\U0001F600";

    internal static void Validate()
    {
        foreach (string name in new[] { NameStart, NameMiddle, NameEnd })
            Require(MjCharacter.GetByMjGlyphName(name)?.MjGlyphName == name, name);
        Require(MjCharacter.GetByMjGlyphName(NameMiss) is null, NameMiss);
        Match(Bmp, "MJ028902", MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CorrespondingUcs);
        Match(Supplementary, "MJ032129", MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CorrespondingUcs);
        Match(Implemented, "MJ030183", MjCharacterMatchKind.ImplementedUcs | MjCharacterMatchKind.CompatibilityIdeograph);
        Match(Corresponding, "MJ000007", MjCharacterMatchKind.CorrespondingUcs);
        Match(Corresponding, "MJ000008", MjCharacterMatchKind.CorrespondingUcs);
        Match(Ivs, "MJ025760", MjCharacterMatchKind.Ivs);
        Match(Svs, "MJ030183", MjCharacterMatchKind.Svs);
        Require(MjCharacter.Find(Multiple).Count >= 2, "multiple");
        Require(MjCharacter.Find(Miss).Count == 0, "miss");
        foreach (string input in Inputs)
        {
            var matches = MjCharacter.Find(input);
            Require(matches.Select(m => m.Entry.MjGlyphName).Distinct().Count() == matches.Count, "merged flags");
            Require(ReferenceEquals(matches, MjCharacter.Find(input)), "shared results");
        }
    }

    internal static readonly string[] Inputs = { Bmp, Supplementary, Implemented, Corresponding, Ivs, Svs, Miss };

    private static void Match(string input, string name, MjCharacterMatchKind kind)
    {
        var matches = MjCharacter.Find(input).Where(m => m.Entry.MjGlyphName == name).ToArray();
        Require(matches.Length == 1 && matches[0].MatchKind == kind, name + " " + kind);
    }

    internal static void Require(bool valid, string description)
    {
        if (!valid) throw new InvalidOperationException("MJ baseline fixture: " + description);
    }
}
