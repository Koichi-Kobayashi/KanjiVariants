// Copyright (c) 2026 Koichi Kobayashi
// Licensed under the MIT License.

using KanjiVariants;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}

var set = CharacterSet.JisX0208;
// 代表的な互換字・補助平面漢字と、候補の重複排除・文字集合フィルターを確認します。
Check(Kanji.GetAlternatives("髙", set).Any(x => x.ToString() == "高"), "髙 candidates");
Check(Kanji.GetAlternatives("𠮷", set).Any(x => x.ToString() == "吉"), "𠮷 candidates");
bool foundMultiple = false;
for (int cp = 0x3400; cp <= 0x9FFF && !foundMultiple; cp++)
{
    if (!System.Text.Rune.IsValid(cp)) continue;
    var source = new System.Text.Rune(cp).ToString();
    if (!KanjiCharacter.TryParse(source, out _)) continue;
    var candidates = Kanji.GetAlternatives(source, set);
    if (candidates.Count < 2) continue;
    Check(candidates.Select(x => x.ToString()).Distinct().Count() == candidates.Count,
        "multiple candidates are distinct");
    Check(candidates.All(x => Kanji.IsSupported(x, set)), "multiple candidates within set");
    foundMultiple = true;
}
Check(foundMultiple, "multiple candidate example found");
Check(Kanji.TryGetAlternative("髙", set, out var alt) && alt.ToString() == "高", "髙 unique");
Check(Kanji.TryGetAlternative("𠮷", set, out alt) && alt.ToString() == "吉", "𠮷 unique");
Check(!Kanji.TryGetAlternative("〻", set, out _), "reject unique target outside JIS X 0208");
Check(KanjiText.Replace("〻", set) == "〻", "retain outside-set target");
Check(KanjiText.Replace("髙橋𠮷野", set) == "高橋吉野", "text replacement");
Check(KanjiText.IsSupported("高橋吉野", set), "result supported");
Check(!KanjiText.IsSupported("髙橋𠮷野", set), "original unsupported");
Check(!KanjiText.IsSupported("①", set), "non-JIS symbol");
Check(KanjiText.IsSupported("ABC123", set), "ASCII");
var unchanged = new string("高橋".ToCharArray());
Check(ReferenceEquals(unchanged, KanjiText.Replace(unchanged, set)), "same string instance");
// IVS/SVS の妥当性、不正UTF-16の維持と走査継続も確認します。
Check(KanjiCharacter.TryParse("𠮷", out _), "supplementary scalar");
Check(!KanjiCharacter.TryParse("A", out _), "reject Latin");
Check(!KanjiCharacter.TryParse("高橋", out _), "reject multiple scalars");
Check(KanjiCharacter.TryParse("\u3404\U000E0101", out _), "registered IVS");
Check(KanjiCharacter.TryParse("\u6B04\uFE00", out _), "registered SVS");
// 2026-08-03版IVDで新規登録されたシーケンスが、生成データから認識できることを確認します。
var newlyRegisteredIvs = new[]
{
    "\u7CA4\U000E0103", "\u805A\U000E0104", "\U00020509\U000E0103",
    "\U00023AA3\U000E0100", "\U00023AA3\U000E0101",
    "\U00026BE7\U000E0100", "\U00026BE7\U000E0101"
};
Check(newlyRegisteredIvs.All(value => KanjiCharacter.TryParse(value, out _)),
    "2026-08-03 IVD additions registered");
Check(!KanjiCharacter.TryParse("髙\uFE0F", out _), "reject unregistered sequence");
Check(KanjiText.Replace("髙\uFE0F", set) == "髙\uFE0F", "preserve unregistered sequence");

// 登録済みだがMJ側に変換先がないシーケンスで、明示的なVSフォールバックを確認します。
var fallback = KanjiFallbackOptions.AllowVariationSelectorFallback;
// 「辻󠄀」は一点しんにょうの字形を指定するIVSです。見えにくいVSの符号位置も確認します。
var tsujiVs = "辻󠄀";
Check(tsujiVs == "辻\U000E0100", "一点しんにょうの辻󠄀はU+8FBB + U+E0100");
Check(KanjiCharacter.TryParse(tsujiVs, out var tsujiCharacter), "registered non-MJ IVS");
Check(Kanji.GetAlternatives(tsujiVs, set).Count == 0, "no default base candidate");
Check(Kanji.GetAlternatives(tsujiVs, set, fallback).Select(x => x.ToString()).SequenceEqual(new[] { "辻" }),
    "IVS base candidate with option");
Check(Kanji.GetAlternatives(tsujiCharacter, set, fallback).Single().ToString() == "辻",
    "typed IVS base candidate with option");
Check(!Kanji.TryGetAlternative(tsujiVs, set, out _), "no default IVS fallback");
Check(Kanji.TryGetAlternative(tsujiVs, set, out alt, fallback) && alt.ToString() == "辻",
    "IVS fallback with option");
Check(Kanji.TryGetAlternative(tsujiCharacter, set, out alt, fallback) && alt.ToString() == "辻",
    "typed IVS fallback with option");
Check(!Kanji.IsSupported(tsujiVs, set) && !KanjiText.IsSupported(tsujiVs, set),
    "IVS remains unsupported as-is");
Check(ReferenceEquals(tsujiVs, KanjiText.Replace(tsujiVs, set)), "default IVS retains string instance");
Check(KanjiText.Replace(tsujiVs, set, fallback) == "辻", "IVS text fallback with option");

// 「榊󠄀」も登録済みIVSですが、MJ一意変換表にはこのシーケンスの変換先がありません。
var sakakiVs = "榊󠄀";
Check(sakakiVs == "榊\U000E0100", "榊󠄀はU+698A + U+E0100");
Check(KanjiCharacter.TryParse(sakakiVs, out _), "registered 榊 IVS");
Check(Kanji.GetAlternatives(sakakiVs, set).Count == 0, "no default 榊 base candidate");
Check(Kanji.GetAlternatives(sakakiVs, set, fallback).Single().ToString() == "榊",
    "榊 base candidate with option");
Check(!Kanji.TryGetAlternative(sakakiVs, set, out _), "no default 榊 fallback");
Check(Kanji.TryGetAlternative(sakakiVs, set, out alt, fallback) && alt.ToString() == "榊",
    "榊 fallback with option");
Check(ReferenceEquals(sakakiVs, KanjiText.Replace(sakakiVs, set)), "default 榊 IVS retains string instance");
Check(KanjiText.Replace(sakakiVs, set, fallback) == "榊", "榊 text fallback with option");

var svs = "不\uFE00";
Check(KanjiCharacter.TryParse(svs, out _), "registered non-MJ SVS");
Check(Kanji.GetAlternatives(svs, set).Count == 0, "no default SVS base candidate");
Check(Kanji.GetAlternatives(svs, set, fallback).Single().ToString() == "不", "SVS base candidate with option");
Check(Kanji.TryGetAlternative(svs, set, out alt, fallback) && alt.ToString() == "不",
    "SVS fallback with option");
Check(KanjiText.Replace(svs, set, fallback) == "不", "SVS text fallback with option");

// 公式の変換先が使える場合はVS除去より優先し、MJ候補と基底文字は重複させません。
var officialVs = "亟\U000E0102";
Check(Kanji.IsSupported("亟", set), "official priority base within set");
Check(Kanji.TryGetAlternative(officialVs, set, out alt, fallback) && alt.ToString() == "丞",
    "official unique target before fallback");
Check(KanjiText.Replace(officialVs, set, fallback) == "丞", "text official target before fallback");
var officialCandidates = Kanji.GetAlternatives(officialVs, set, fallback).Select(x => x.ToString()).ToArray();
Check(officialCandidates.Contains("亟") && officialCandidates.Contains("丞") &&
    officialCandidates.Distinct().Count() == officialCandidates.Length, "MJ and fallback candidates distinct");
Check(KanjiText.Replace("亟", set) == "亟", "supported source remains unchanged");
var mjTsujiVs = "辻\U000E0102";
var mjCandidates = Kanji.GetAlternatives(mjTsujiVs, set, fallback).Select(x => x.ToString()).ToArray();
Check(mjCandidates.Count(x => x == "辻") == 1, "MJ base candidate not duplicated by fallback");

// 各行の先頭はVSなし、以降は表に記載された連続するVS付きの表現です。
void CheckVariationRow(string row, string baseCharacter, int firstSelector)
{
    var examples = row.Split('　');
    Check(examples[0] == baseCharacter && Kanji.IsSupported(baseCharacter, set),
        $"{baseCharacter} is supported without VS");
    for (int i = 1; i < examples.Length; i++)
    {
        int selector = firstSelector + i - 1;
        string example = examples[i];
        string label = $"{baseCharacter} + U+{selector:X}";
        Check(KanjiCharacter.TryParse(example, out var parsed) &&
            parsed.BaseCharacter.Value == char.ConvertToUtf32(baseCharacter, 0) &&
            parsed.VariationSelector?.Value == selector, $"{label} registered and correctly encoded");
        Check(!Kanji.IsSupported(example, set), $"{label} is unsupported as a sequence");
        // この一覧のIVSにはMJの一意な変換先があるため、オプションなしでも基底文字へ縮退します。
        Check(Kanji.TryGetAlternative(example, set, out var alternative) &&
            alternative.ToString() == baseCharacter, $"{label} official alternative");
        Check(KanjiText.Replace(example, set) == baseCharacter, $"{label} official replacement");
        Check(KanjiText.Replace(example, set, fallback) == baseCharacter,
            $"{label} replacement with fallback option");
    }
}

CheckVariationRow(
    "邉　邉󠄏　邉󠄐　邉󠄑　邉󠄒　邉󠄓　邉󠄔　邉󠄕　邉󠄖　邉󠄗　邉󠄘　邉󠄙　邉󠄚　邉󠄛　邉󠄜　邉󠄝",
    "邉", 0xE010F);
CheckVariationRow(
    "邊　邊󠄈　邊󠄉　邊󠄊　邊󠄋　邊󠄌　邊󠄍　邊󠄎　邊󠄏　邊󠄐",
    "邊", 0xE0108);

var outsideBaseVs = "\u3404\U000E0101";
Check(Kanji.GetAlternatives(outsideBaseVs, set, fallback).SequenceEqual(Kanji.GetAlternatives(outsideBaseVs, set)),
    "outside-set base not added to candidates");
Check(!Kanji.TryGetAlternative(outsideBaseVs, set, out _, fallback), "outside-set base not used as fallback");
Check(KanjiText.Replace(outsideBaseVs, set, fallback) == outsideBaseVs,
    "outside-set base sequence retained");
Check(KanjiText.Replace("髙\uFE0F𠮷", set, fallback) == "髙\uFE0F吉",
    "unregistered sequence retained with option and scan continues");
Check(!KanjiText.IsSupported("高\uFE0F", set), "variation sequence unsupported");
Check(!KanjiText.IsSupported("高\ud800", set), "invalid UTF-16 unsupported");
Check(KanjiText.Replace("髙\ud800𠮷", set) == "高\ud800吉", "preserve invalid UTF-16 and continue");
Check(KanjiText.Replace("", set) == "", "empty replace");
Check(KanjiText.IsSupported("", set), "empty supported");
try
{
    KanjiText.Replace(null!, set);
    throw new Exception("null replace did not throw");
}
catch (ArgumentNullException) { Console.WriteLine("PASS null replace"); }
