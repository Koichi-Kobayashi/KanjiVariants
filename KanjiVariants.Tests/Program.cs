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
Check(!KanjiCharacter.TryParse("髙\uFE0F", out _), "reject unregistered sequence");
Check(KanjiText.Replace("髙\uFE0F", set) == "髙\uFE0F", "preserve unregistered sequence");
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
