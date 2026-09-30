# MJ文字情報

`MjCharacter` はMJ文字情報一覧表 Ver.006.02の58,862文字図形を参照します。

| API・型 | 役割 |
|---|---|
| `GetByMjGlyphName` | 原典の識別子であるMJ文字図形名から一意取得 |
| `Find` | Unicode表現から関連MJ文字を複数検索 |
| `MjCharacterEntry` | 一つのMJ文字図形の原典情報 |
| `MjCharacterMatch` | 一致したEntryと検索理由 |
| `MjCharacterMatchKind` | Flagsによる一致理由 |
| `MjKanjiPolicy` | 原典の漢字施策（Joyo / Jinmeiyo） |

```csharp
using KanjiVariants;

var entry = MjCharacter.GetByMjGlyphName("MJ028902"); // 髙
foreach (var match in MjCharacter.Find("髙"))
    Console.WriteLine($"{match.Entry.MjGlyphName}: {match.MatchKind}");
```

## 原典情報の区別

- `CorrespondingUcs`（対応するUCS）と `ImplementedUcs`（実装したUCS）は異なる概念として、別の nullable Rune で保持します。
- `Ivs` は実装したMoji_JohoコレクションIVS、`Svs` は実装したSVSです。どちらも `IReadOnlyList<KanjiCharacter>` で複数シーケンスを原典順に保持し、空欄は共有の空一覧です。
- `JisX0213` はX0213列の文字列原典値です。CharacterSet判定とは自動結合しません。
- `KanjiPolicy` は原典の漢字施策で、空欄はnullです。既存の常用・人名用漢字APIとは自動統合しません。
- `CompatibilityIdeograph` は対応する互換漢字です。対応UCSとは同一視しません。

`Find` はすべてのUCS・IVS・SVS・互換漢字を検索します。同じEntryに複数理由で一致した場合は、理由をビットORした1件にまとめます。例えば「髙」のMJ028902では `ImplementedUcs | CorrespondingUcs` が返ります。戻り値は共有の読み取り専用一覧です。

`MJ059399` と `MJ059400` にはIVSが2件ずつあります。全シーケンスから検索できます。IVS/SVSを基底文字へ自動縮退せず、基底文字とは別に検索します。

文字図形名は大文字小文字を区別する完全一致です。該当なしはnullです。`Find(string)` は々・〆・〻も含むUnicodeスカラー一文字か、登録済みIVS/SVSを受け付けます。無効な文字列・該当なしは空一覧、null入力は例外です。

関連API: <xref:KanjiVariants.MjCharacter>・<xref:KanjiVariants.MjCharacterEntry>・<xref:KanjiVariants.MjCharacterMatch>・<xref:KanjiVariants.MjCharacterMatchKind>・<xref:KanjiVariants.MjKanjiPolicy>
