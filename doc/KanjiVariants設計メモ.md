# KanjiVariants 設計メモ

更新日: 2026-09-23


## 1. 概要

`KanjiVariants` は、日本語の漢字について、指定された文字集合で使用可能な
代替文字の検索・置換を行う .NET ライブラリ。

主なユースケースは、古いシステムや外部システムなど、
使用可能な文字に制限がある環境へ文字列を渡す際の代替文字処理。

例:

- `髙` → `高`
- `𠮷` → `吉`

単なる Shift_JIS エンコーディング変換ライブラリではなく、

> 指定された文字集合で使用可能な代替文字を、
> 公的な文字情報・縮退情報に基づいて検索する

ことを目的とする。


---

## 2. パッケージ名 / 名前空間

```csharp
KanjiVariants
```

を使用する。

過去に `Kanji.NET` というNuGetパッケージを公開していたため、
`Kanji.NET` は再利用しない。

V2以降で漢字以外の互換文字を扱う可能性はあるが、
ライブラリの中心は漢字であるため `KanjiVariants` の名称は維持する。


---

## 3. ターゲットフレームワーク

V1では、最低対応バージョンを .NET 6 とする。

```xml
<TargetFramework>net6.0</TargetFramework>
```

.NET 10専用APIなどを使用せず、
.NET 6で利用可能なAPIの範囲で実装する。

`net6.0` 向けにビルドしたライブラリであっても、
.NET 10アプリケーションから使用する場合は、
.NET 10のランタイムおよびJITで実行される。

そのため、V1ではマルチターゲット化せず、
`net6.0` の単一ターゲットとする。

将来、より新しい.NET専用APIを利用することで
明確な性能上または機能上の利点が得られる場合は、

```xml
<TargetFrameworks>net6.0;net10.0</TargetFrameworks>
```

のようなマルチターゲット化を検討する。


---

## 4. V1 のスコープ

V1では以下を対象とする。

- 指定文字集合で使用可能な代替候補の取得
- 一意に決定された代替文字の取得
- 文字列中の漢字を代替文字へ置換
- 指定文字集合で文字・文字列を使用可能か判定
- Unicode補助平面の漢字への対応
- IVS / SVSを考慮した漢字表現

V1では以下は行わない。

- 一般的な意味での「異体字一覧」の提供
- MJ縮退マップの根拠情報をPublic APIから取得する機能
- 丸数字など、漢字以外の互換文字変換
- 全角 / 半角変換
- 業務固有の文字変換ルール

MJ縮退マップの根拠情報は内部データとして保持してもよいが、
V1のPublic APIには露出しない。


---

## 5. `GetVariants()` は採用しない

当初、

```csharp
Kanji.GetVariants("髙");
```

というAPIを検討していた。

しかし「異体字」の範囲が曖昧である。

例えば以下は性質が異なる。

- IVSによる字形差
- SVS
- Unicode互換漢字
- 戸籍上の親字・正字
- JIS包摂関係
- 辞書上の関連字
- 読み・字形から類推された文字

MJ縮退マップを逆引きして、同じ縮退先を持つ文字を
すべて「異体字」と扱うことも適切ではない。

そのためV1では `GetVariants()` を公開しない。

必要性が明確になった時点で、改めて意味を定義して追加する。


---

## 6. APIの責務

APIは大きく2つに分ける。

### `Kanji`

1つの漢字を対象とする。

```csharp
Kanji.GetAlternatives(...)
Kanji.TryGetAlternative(...)
Kanji.IsSupported(...)
```

### `KanjiText`

文字列全体を対象とする。

```csharp
KanjiText.Replace(...)
KanjiText.IsSupported(...)
```

これにより、

> Kanji = 1文字を調べる  
> KanjiText = 文字列を処理する

という分かりやすい責務分担にする。


---

## 7. Public API（現時点の案）

V1では、公開APIを必要最小限に絞る。

公開する主な型は以下とする。

```text
Kanji
KanjiText
KanjiCharacter
CharacterSet
```

MJ縮退マップの根拠情報や内部的な候補情報は、
V1では公開APIに含めない。

```csharp
namespace KanjiVariants;

public static class Kanji
{
    public static IReadOnlyList<KanjiCharacter> GetAlternatives(
        KanjiCharacter character,
        CharacterSet characterSet);

    public static IReadOnlyList<KanjiCharacter> GetAlternatives(
        string character,
        CharacterSet characterSet);

    public static bool TryGetAlternative(
        KanjiCharacter character,
        CharacterSet characterSet,
        out KanjiCharacter alternative);

    public static bool TryGetAlternative(
        string character,
        CharacterSet characterSet,
        out KanjiCharacter alternative);

    public static bool IsSupported(
        KanjiCharacter character,
        CharacterSet characterSet);

    public static bool IsSupported(
        string character,
        CharacterSet characterSet);
}

public static class KanjiText
{
    public static string Replace(
        string text,
        CharacterSet characterSet);

    public static bool IsSupported(
        string text,
        CharacterSet characterSet);
}
```


### Public APIを最小限にする方針

当初は、代替候補とその根拠を表すために、

```text
KanjiAlternative
AlternativeEvidence
AlternativeRelation
AlternativeEvidenceDetail
AlternativePriority
```

などの型を公開する案を検討していた。

しかし、V1の主目的は、

```text
この漢字の代替文字を取得する
文字列を代替可能な文字へ置換する
指定された文字集合で使用可能か判定する
```

ことであり、利用者がMJ縮退マップの詳細な根拠情報まで
必要とするケースは限定的と考えられる。

そのため、V1では `GetAlternatives()` の戻り値を、

```csharp
IReadOnlyList<KanjiCharacter>
```

とする。

例:

```csharp
var alternatives = Kanji.GetAlternatives(
    "髙",
    CharacterSet.JisX0208);

foreach (var alternative in alternatives)
{
    Console.WriteLine(alternative);
}
```

MJ縮退マップ上の根拠情報は内部データとして保持してもよいが、
V1では公開しない。

これにより、

- Public APIを小さく保てる
- MJ縮退マップの内部構造をPublic APIへ露出しない
- 将来、根拠情報の持ち方を変更しても互換性への影響を抑えられる
- NuGet利用者が必要以上に内部データ構造を意識しなくてよい

という利点がある。

将来、代替候補の根拠情報を取得する需要が明確になった場合は、

```csharp
Kanji.GetAlternativeDetails(...)
```

や、

```csharp
Kanji.GetAlternativesWithEvidence(...)
```

などの高度なAPIを追加することを検討する。

V1では追加しない。

## 内部実装

MJ縮退マップの候補や根拠情報は、
V1ではPublic APIへ露出せず、内部データとして保持する。

ランタイムでは、元のJSON構造をそのままオブジェクト化せず、
Build-time Generatorによって検索に適したフラットなデータへ変換する。


### 代替候補

1つのMJ文字には複数の代替候補が存在する場合がある。

実データでは候補数は最大8件であるため、
候補数は `byte` で保持できる。

内部的には、Entryごとに候補配列の開始位置と件数を保持する。

概念例:

```text
EntryIndex
    ↓
AlternativeStart
AlternativeCount
    ↓
AlternativeIndex
```

候補文字はUnicode scalar valueを `int` で保持する。

MJ縮退マップの代替候補および
MJ縮退マップ 一意な変換表の変換先を確認した結果、
変換先は単一のUnicode scalarであり、
IVS / SVSのような複数コードポイント表現は存在しない。

そのため、代替候補側では `ulong UnicodeKey` を使用せず、
Unicode scalar valueを直接保持する。

概念例:

```csharp
private static readonly int[] s_uniqueAlternativeCodePoints;
private static readonly int[] s_alternativeCodePoints;
```

一意な変換先については、

```text
0 = 代替文字なし
```

とする。

MJ縮退マップ 一意な変換表に存在する
「候補なし」を示す U+FF3F は、
Build-time Generatorで `0` に変換する。

これにより、ランタイム側では
MJ固有の「候補なし」表現を意識しない。


### 根拠情報

1つの代替候補には、複数の根拠が存在する場合がある。

根拠情報は内部では `Evidence` として扱う。

主な分類は以下。

```text
JIS包摂規準・UCS統合規則
法務省戸籍法関連通達・通知
法務省告示582号別表第四
辞書類等による関連字
読み・字形による類推
```

「参考情報」は代替候補が成立した根拠とは性質が異なるため、
Evidenceには含めない。

内部的な分類は、例えば以下のようなenumで表現できる。

```csharp
internal enum AlternativeRelation : byte
{
    JisUnification = 0,
    FamilyRegister,
    Notification582,
    Dictionary,
    GlyphOrReadingInference
}
```

戸籍関係の詳細については、文字列を各Evidenceに保持せず、
enumとして保持する。

```csharp
internal enum EvidenceDetail : byte
{
    None = 0,
    FamilyRegisterParentOrOfficial,
    Notification2842,
    Notification5202
}
```

表示やデバッグ等で元資料名が必要な場合は、
以下の日本語名称へ変換する。

```text
FamilyRegisterParentOrOfficial
→ 戸籍統一文字情報 親字・正字

Notification2842
→ 民一2842号通達別表 誤字俗字・正字一覧表

Notification5202
→ 民二5202号通知別表 正字・俗字等対照表
```

法務省告示582号別表第四の順位情報は、
内部では以下のように扱う。

```csharp
internal enum AlternativePriority : byte
{
    None = 0,
    First = 1,
    Second = 2
}
```

ホップ数は、

```text
0 = なし
1以上 = MJ縮退マップ上のホップ数
```

として `byte` で保持する。

実データ上のホップ数は最大5であり、
`byte` で十分に表現できる。


### Evidenceの内部表現

概念的には以下のような固定長データとして保持する。

```csharp
internal readonly struct EvidenceEntry
{
    public AlternativeRelation Relation { get; init; }

    public EvidenceDetail Detail { get; init; }

    public byte HopCount { get; init; }

    public AlternativePriority Priority { get; init; }
}
```

1つの候補に複数のEvidenceが存在するため、
候補ごとにEvidence配列の開始位置と件数を保持する。

```text
AlternativeIndex
    ↓
EvidenceStart
EvidenceCount
    ↓
EvidenceEntry[]
```

Evidence数も実データ上は最大5件であるため、
`EvidenceCount` は `byte` で保持できる。


### 内部データの型方針

内部配列では、以下を基本方針とする。

```text
Count      → byte
Index      → int
Start      → int
CodePoint  → int
Key        → ulong
Flags      → byte基底のenum
```

`Key` は主にIVS / SVSの複合キーで使用する。

`ushort` で収まる値であっても、
IndexやStartには `int` を使用する。

これにより、将来のデータ更新でも上限を意識せずに済む。


### Unicode検索構造

通常のUnicode scalarと、IVS / SVSでは検索方法を分ける。

#### 通常のUnicode scalar

VSを伴わない通常のUnicode scalarについては、
コードポイントから直接 `EntryIndex` を取得できる
Direct Lookup Tableを使用する。

概念的には以下のようになる。

```text
Rune.Value
    ↓
Unicode範囲判定
    ↓
Direct Lookup Table
    ├─ EntryIndex
    └─ CharacterSetFlags
```

主なCJK Unicode範囲ごとにDirect Lookup Tableを生成する。

例:

```text
U+3400  ～ U+4DBF    CJK Unified Ideographs Extension A
U+4E00  ～ U+9FFF    CJK Unified Ideographs
U+F900  ～ U+FAFF    CJK Compatibility Ideographs
U+20000 ～ U+2A6DF   Extension B
U+2A700 ～ U+2B73F   Extension C
U+2B740 ～ U+2B81F   Extension D
U+2B820 ～ U+2CEAF   Extension E
U+2CEB0 ～ U+2EBEF   Extension F
```

存在しないコードポイントの `EntryIndex` には `-1` を使用する。

通常UCSについては、

```text
UnicodeKey
    ↓
UnicodeKeyIndex
    ↓
EntryIndex
```

という中間層を設けず、

```text
Unicode scalar
    ↓
EntryIndex
```

へ直接解決する。


#### IVS / SVS

IVS / SVSについては、
基底文字とVariation Selectorの組み合わせを複合キーとして検索する。

複合キーは `ulong` とし、

```text
bit  0 ～ 20 : BaseCharacter
bit 21 ～ 41 : VariationSelector
bit 42 ～ 63 : 未使用
```

とする。

Variation Selectorなしを表す値として `0` を使用する。

```csharp
private static ulong CreateKey(
    Rune baseCharacter,
    Rune? variationSelector)
{
    const int UnicodeBits = 21;

    return (uint)baseCharacter.Value
        | ((ulong)(uint)(variationSelector?.Value ?? 0) << UnicodeBits);
}
```

IVS / SVS用キーは昇順に保持し、
`Array.BinarySearch()` で検索する。

概念的には以下。

```text
BaseCharacter + VariationSelector
    ↓
ulong UnicodeKey
    ↓
BinarySearch
    ├─ EntryIndex
    └─ CharacterSetFlags
```

V1では自前のBinary Searchは実装せず、
まず `Array.BinarySearch()` を使用する。

必要であればBenchmarkDotNetによる計測後に最適化する。


### CharacterSetFlags

文字集合への所属情報は、内部ではFlagsとして保持する。

```csharp
[Flags]
internal enum CharacterSetFlags : byte
{
    None = 0,
    JisX0208 = 1 << 0,
}
```

公開APIの `CharacterSet` と、
内部の所属情報である `CharacterSetFlags` は分離する。

将来、

```csharp
CharacterSet.JisX0213
```

などを追加した場合も、
内部Flagsを追加することで複数文字集合への所属を表現できる。


### 内部Lookup API

通常UCSとIVS / SVSの検索方法の違いは、
内部Lookup APIで隠蔽する。

```csharp
private static bool TryLookup(
    Rune baseCharacter,
    Rune? variationSelector,
    out int entryIndex,
    out CharacterSetFlags flags);
```

通常UCSではDirect Lookup、
IVS / SVSでは複合キーによるBinary Searchを使用する。

Public APIで `KanjiCharacter` を扱う場合も、
文字列走査中のホットパスも、
最終的にはこのLookup処理を共通利用する。


### MJ縮退マップの「参考情報」

MJ縮退マップには、

```text
参考情報
```

も存在する。

例:

```text
地名外字
```

これは代替候補が成立した根拠とは性質が異なるため、
Evidenceには含めない。

将来、文字そのものの詳細情報を公開するAPIが必要になった場合は、

```csharp
Kanji.GetInfo(...)
```

などの別APIで補足情報として扱うことを検討する。

V1ではPublic APIとして提供しない。

---

## 8. `GetAlternatives()`

```csharp
Kanji.GetAlternatives(
    "髙",
    CharacterSet.JisX0208);
```

指定された文字集合で使用可能な代替候補を取得する。

主なデータソースは、

> MJ縮退マップ

とする。

候補が複数存在する可能性があるため、
戻り値はリストとする。

```csharp
IReadOnlyList<KanjiCharacter>
```

例:

```csharp
var alternatives = Kanji.GetAlternatives(
    "髙",
    CharacterSet.JisX0208);

foreach (var alternative in alternatives)
{
    Console.WriteLine(alternative);
}
```

`GetAlternatives()` は「最適な1文字」を決定するAPIではない。

MJ縮退マップに存在する候補のうち、
指定された `CharacterSet` で使用可能な代替文字を返す。

同じ代替文字が複数の根拠から得られる場合でも、
戻り値には同じ文字を重複して含めない。

MJ縮退マップ上の根拠情報は内部データとして保持してもよいが、
V1のPublic APIでは公開しない。

一意に決定された代替文字が必要な場合は、

```csharp
Kanji.TryGetAlternative(...)
```

を使用する。

---

## 9. `TryGetAlternative()`

```csharp
Kanji.TryGetAlternative(
    "髙",
    CharacterSet.JisX0208,
    out var alternative);
```

一意に決定された代替文字を取得する。

主なデータソースは、

> MJ縮退マップ 一意な変換表

とする。

したがって、

```csharp
GetAlternatives(...)
```

の結果が1件だった場合に返す、という意味ではない。

MJ縮退マップに複数候補が存在していても、
「一意な変換表」で変換先が定義されていれば取得できる。

例:

```text
髙 → 高
𠮷 → 吉
```

XMLコメント等では、

> MJ縮退マップ 一意な変換表に基づいて決定される

ことを明記する。


---

## 10. `KanjiText.Replace()`

基本形:

```csharp
var result = KanjiText.Replace(
    "髙橋𠮷野",
    CharacterSet.JisX0208);
```

結果:

```text
高橋吉野
```

`TryGetAlternative()` と同様に、
一意な変換表を使用して文字列全体を処理する。


### 代替文字が存在しない場合

元の文字をそのまま残す。

```text
変換可能     → 代替文字へ置換
変換不可能   → 元の文字を維持
```

例外にはしない。

`?` や `＿` などへ勝手に置換しない。

MJ縮退マップ 一意な変換表では、
「候補なし」を示すために U+FF3F（全角アンダースコア）が
変換先として格納されているデータがあるが、
KanjiVariantsではこれを実際の代替文字として使用しない。

「候補なし」は「代替文字なし」と解釈する。


### 文字列走査

文字列はUTF-16の `char` 単位ではなく、
`Rune` 単位で走査する。

Unicode補助平面の文字も1つのUnicode scalarとして扱う。

```text
高   → 1 Rune
𠮷   → 1 Rune（UTF-16では2コードユニット）
```

直後にVariation Selectorが存在する場合は、

```text
BaseCharacter + VariationSelector
```

を1つの論理単位として扱う。


### IVS / SVSの扱い

登録済みのIVS / SVSは、
基底文字とVariation Selectorを組み合わせて検索する。

未登録の、

```text
BaseCharacter + VariationSelector
```

を検出した場合は、
BaseCharacterだけにフォールバックして置換しない。

シーケンス全体を元のまま残す。

Variation Selectorだけを削除する処理も行わない。


### 不正なUTF-16

`KanjiText.Replace()` は、
文字列中に不正なUTF-16シーケンスが存在しても
文字列全体を例外にはしない。

不正部分は元のUTF-16コードユニットをそのまま残し、
残りの文字列の処理を継続する。


### ASCII Fast Path

ASCII文字は置換対象にならないため、
Lookupを行わずそのまま通過させる。


### 出力文字列の生成

置換が1件も発生しなかった場合は、
新しい文字列を生成せず、
入力された `string` インスタンスをそのまま返す。

`StringBuilder` は最初の置換が発生した時点で初めて生成する。

概念的には、

```text
置換なし
→ StringBuilder生成なし
→ 元のstringを返す

置換あり
→ 最初の置換時にStringBuilder生成
→ それまでの文字列をコピー
→ 以降を構築
```

とする。


---

## 11. `IsSupported()`

### 1文字

```csharp
Kanji.IsSupported(
    "高",
    CharacterSet.JisX0208);
```

### 文字列

```csharp
KanjiText.IsSupported(
    text,
    CharacterSet.JisX0208);
```

`KanjiText.IsSupported()` は漢字だけではなく、
文字列全体を検証する。

つまり、

```text
漢字
英数字
記号
丸数字
その他の文字
```

すべてを対象とする。

典型的な利用方法:

```csharp
var result = KanjiText.Replace(
    text,
    CharacterSet.JisX0208);

if (!KanjiText.IsSupported(
    result,
    CharacterSet.JisX0208))
{
    // 代替できなかった文字や、
    // 対象文字集合で使用できない文字が残っている
}
```

責務は、

> Replace = 置換できるものを置換する  
> IsSupported = 最終結果を検証する

とする。


### 文字列走査時の判定

`KanjiText.IsSupported()` は
`KanjiText.Replace()` と同じRune走査ルールを使用する。

```text
ASCII
→ 使用可能として通過

通常のUnicode scalar
→ Direct LookupでCharacterSetFlagsを確認

登録済みIVS / SVS
→ シーケンスそのもののCharacterSetFlagsを確認

未登録の Base + VS
→ false

不正なUTF-16
→ false
```

`CharacterSet.JisX0208` では、
Variation Selectorを含むシーケンスそのものは
JIS X 0208に存在しないため `false` となる。

`Replace()` と `IsSupported()` の責務は引き続き、

```text
Replace
→ 置換可能な文字だけを置換する

IsSupported
→ 最終的な文字列が対象文字集合で使用可能か検証する
```

とする。


---

## 12. `CharacterSet`

V1ではまず以下のみ提供する。

```csharp
public enum CharacterSet
{
    /// <summary>
    /// JIS X 0208。
    /// 第1水準および第2水準の漢字を含みます。
    /// </summary>
    JisX0208
}
```

`JisX0208` は利用者にとって、

> JIS第1水準・第2水準を含む文字集合

として理解できるよう、ドキュメントで説明する。

注意:

JIS X 0213の「第1面 / 第2面」と、
「JIS第1水準 / 第2水準」は別の概念。

```text
第1水準 ≠ 第1面
第2水準 ≠ 第2面
```

### JIS X 0213 の第1面 / 第2面

JIS X 0213 は、第1面と第2面の2つの面で構成される。

概念的には、以下のような関係になる。

```text
JIS X 0213
├─ 第1面
│  ├─ JIS X 0208由来の文字
│  └─ JIS X 0213で追加された文字
│
└─ 第2面
   └─ JIS X 0213で追加された漢字
```

そのため、

```text
第1面 = JIS X 0208
```

ではない。

JIS X 0213の第1面には、
JIS X 0208由来の文字だけでなく、
JIS X 0213で追加された文字も含まれる。

JIS X 0213の面区点位置は、例えば以下のように表される。

```text
1-25-66
```

これは、

```text
1  = 第1面
25 = 区
66 = 点
```

を意味する。

同様に、

```text
2-82-56
```

であれば、

```text
2  = 第2面
82 = 区
56 = 点
```

となる。

したがって、`CharacterSet.JisX0208` の判定では、

```text
第1面に存在するか
```

だけを条件にしてはいけない。

JIS X 0213の第1面には、
JIS X 0208には含まれない追加文字も存在するため、

> JIS X 0208に実際に収録されている文字かどうか

を判定する必要がある。


### MJ縮退マップ 一意な変換表と `CharacterSet.JisX0208`

MJ縮退マップ 一意な変換表の変換先は、
JIS X 0213の面区点位置を基準としている。

そのため、一意な変換表に変換先が存在するだけでは、
その文字を `CharacterSet.JisX0208` で使用可能とは判断できない。

例えば、

```csharp
Kanji.TryGetAlternative(
    character,
    CharacterSet.JisX0208,
    out var alternative);
```

および、

```csharp
KanjiText.Replace(
    text,
    CharacterSet.JisX0208);
```

では、概念的に以下の処理を行う。

```text
MJ縮退マップ 一意な変換表
        ↓
一意な変換先を取得
        ↓
指定された CharacterSet に含まれるか判定
        ↓
Yes → 代替文字として採用
No  → 使用可能な代替文字なしとして扱う
```

MJ縮退マップ 一意な変換表には、
「候補なし」を除いて 27,597件の一意な変換先が存在する。

このうち、JIS X 0208に含まれるものは 20,062件であり、
残りの 7,535件は JIS X 0208の範囲外となる。

```text
一意な変換先あり        27,597件
├─ JIS X 0208内          20,062件
└─ JIS X 0208外           7,535件
```

JIS X 0208外の 7,535件は、
JIS X 0213上では以下のように分かれる。

```text
JIS X 0213 第1面だがJIS X 0208外    2,815件
JIS X 0213 第2面                     4,720件
```

この結果からも、

```text
JIS X 0213 第1面 = JIS X 0208
```

とは扱えないことが分かる。

そのためKanjiVariantsでは、
JIS X 0208の所属情報そのものをビルド時に生成し、
`CharacterSet.JisX0208` の判定に使用する。

WindowsのCP932やShift_JISでエンコード可能かどうかを、
そのままJIS X 0208の所属判定として使用しない。

将来的に必要であれば、

```csharp
CharacterSet.JisX0213
```

などを追加できる。


---

## 13. `KanjiCharacter`

.NETの `char` はUTF-16コードユニットであり、
必ずしもUnicode上の1文字を表さない。

例えば、

```text
𠮷 U+20BB7
```

はUTF-16ではサロゲートペアになる。

さらにIVSは、

```text
基底漢字 + Variation Selector
```

という複数コードポイントから構成される。

そのため、公開APIでは独自の

```csharp
KanjiCharacter
```

を使用する。

現時点の案:

```csharp
public readonly record struct KanjiCharacter
{
    public Rune BaseCharacter { get; }

    public Rune? VariationSelector { get; }

    public bool HasVariationSelector =>
        VariationSelector is not null;

    public static KanjiCharacter Parse(string value);

    public static bool TryParse(
        string value,
        out KanjiCharacter character);

    public override string ToString();
}
```


---

## 14. `KanjiCharacter` が受け付けるもの

例:

```text
高          OK
髙          OK
𠮷          OK
登録済IVS   OK
登録済SVS   OK

A           NG
①           NG
あ          NG
VS単独       NG
高橋         NG
未登録の漢字 + VS  NG
```

`KanjiCharacter` は、

> Unicode上の任意の1文字

ではなく、

> KanjiVariantsが認識可能な、妥当な漢字1文字の表現

を表す。


---

## 15. IVS / SVS

IVSは最初から考慮する。

単純に、

```text
漢字 + Variation Selector
```

という構造になっているだけでは有効としない。

Unicodeに正式登録されたVariation Sequenceのみを
有効な `KanjiCharacter` とする。


### IVS

Unicode IVD
(Ideographic Variation Database)
に登録されたものを使用する。

Moji_Johoだけに限定せず、
Unicode IVDに正式登録されたCollectionのIVSを
原則として受け入れる。


### SVS

UnicodeのStandardized Variation Sequenceとして
登録されたものを受け入れる。


### 責務の分離

```text
KanjiCharacter
    ↓
漢字として有効なUnicode表現か

Kanji.GetAlternatives()
    ↓
その文字について代替情報を持っているか
```

そのため、有効なIVSであってもMJ側に対応情報がなければ、
`GetAlternatives()` が空になることはあり得る。


---


## 16. データソース

### MJ文字情報一覧表

主に以下の文字情報を取得する。

- MJ文字図形名
- UCS
- IVS
- SVS
- 互換漢字
- JIS X 0213
- 戸籍統一文字番号
- 登記統一文字番号
- その他文字メタデータ

使用予定:

```text
MJ文字情報一覧表 Ver.006.02
```


### MJ縮退マップ

使用予定:

```text
MJ縮退マップ Ver.1.2.0
```

主に、

```csharp
Kanji.GetAlternatives(...)
```

で使用する。

候補には複数の根拠が存在する場合がある。

候補数が複数になるMJ文字も多数存在するため、
独自に「先頭候補」を代替文字として採用してはいけない。


### MJ縮退マップ 一意な変換表

使用予定:

```text
MJ縮退マップ 一意な変換表 Ver.1.2.0
```

主に、

```csharp
Kanji.TryGetAlternative(...)
KanjiText.Replace(...)
```

で使用する。

複数候補からKanjiVariants独自の優先順位を作るのではなく、
公式の一意な変換先を利用する。


### Unicode IVD

IVSの妥当性判定に使用する。

Moji_Johoを含むUnicode IVD Collectionの
正式登録されたIdeographic Variation Sequenceを扱う。


### Unicode Standardized Variants

SVSの妥当性判定に使用する。


---

## 17. 確認済みの代表例

### 髙

```text
髙
U+9AD9
MJ028902
```

MJ縮退マップでは、

```text
高
U+9AD8
```

への関係が存在する。

根拠には、

- JIS包摂規準・UCS統合規則
- 戸籍統一文字情報 親字・正字

などが存在する。

一意な変換表:

```text
髙 → 高
```


### 𠮷

```text
𠮷
U+20BB7
MJ032129
```

縮退先:

```text
吉
U+5409
```

複数の根拠が存在する。

一意な変換表:

```text
𠮷 → 吉
```


---

## 18. データのバージョン関係

MJ文字情報一覧表 Ver.006.02 のリリース情報では、
以下との対応が明記されている。

```text
MJ文字情報一覧表           Ver.006.02
IPAmj明朝                  Ver.006.01
MJ縮退マップ               Ver.1.2.0
MJ縮退マップ 一意な変換表  Ver.1.2.0
登記統一文字縮退マップ     Ver.1.0.0
```

したがって、

```text
MJ文字情報一覧表 Ver.006.02
+
MJ縮退マップ Ver.1.2.0
+
MJ縮退マップ 一意な変換表 Ver.1.2.0
```

の組み合わせを使用できる。


---

## 19. データの組み込み方針

ビルド時に必要な情報だけを抽出・変換し、
ランタイムでは生成済みのフラットな配列を使用する。

V1では、Build-time Generatorが
C#ソースコードを生成する方式を採用する。

生成対象は概ね以下。

```text
通常UCS
├─ Direct Lookup EntryIndex
└─ CharacterSetFlags

IVS / SVS
├─ ulong VariationKey
├─ EntryIndex
└─ CharacterSetFlags

Entry
├─ UniqueAlternativeCodePoint
├─ AlternativeStart
└─ AlternativeCount

Alternative
├─ AlternativeCodePoint
├─ EvidenceStart
└─ EvidenceCount

Evidence
└─ EvidenceEntry[]
```

型は原則として以下を使用する。

```text
Count      → byte
Index      → int
Start      → int
CodePoint  → int
Key        → ulong
Flags      → byte基底のenum
```

Evidenceは複数項目をまとめて参照するため、
並列配列ではなく小さな `readonly struct` 配列として保持する。

```csharp
internal readonly struct EvidenceEntry
{
    public AlternativeRelation Relation { get; init; }
    public EvidenceDetail Detail { get; init; }
    public byte HopCount { get; init; }
    public AlternativePriority Priority { get; init; }
}
```

生成C#ソース方式を採用することで、

- 実行時のExcel / JSON読み込みを不要にする
- デシリアライズを不要にする
- 外部依存を増やさない
- 生成結果をデバッグしやすくする

ことを優先する。

将来、生成ソースのサイズやコンパイル時間が問題になった場合は、
バイナリリソース方式への変更を検討する。

---

## 20. テスト方針

V1では、Public APIの挙動だけでなく、
Unicode境界条件、IVS / SVS、不正UTF-16、内部最適化方針も含めてテストする。

主なテスト対象は以下。


### `Kanji.GetAlternatives()`

- 代表例 `髙`
- 代表例 `𠮷`
- 複数候補を持つ文字
- 候補なし
- 同じ候補が複数の根拠から得られても重複して返さない
- 指定された `CharacterSet` 外の候補を返さない
- 登録済みIVS / SVS
- MJ側に代替情報を持たない有効な `KanjiCharacter`


### `Kanji.TryGetAlternative()`

- `髙 → 高`
- `𠮷 → 吉`
- 一意な変換先なし
- 一意な変換先は存在するが、指定された `CharacterSet` 外
- MJ縮退マップに複数候補が存在していても、
  一意な変換表に変換先が定義されていれば取得できる
- `GetAlternatives().Count == 1` を判定条件として使用しない


### `Kanji.IsSupported()`

- JIS X 0208内の文字
- JIS X 0208外の文字
- 補助平面の漢字
- 登録済みIVS / SVS
- 未登録の `BaseCharacter + VariationSelector`


### `KanjiText.Replace()`

- `髙橋𠮷野 → 高橋吉野`
- 複数文字を連続して置換
- 代替文字なしの場合は元の文字を維持
- 一意変換先が対象 `CharacterSet` 外の場合は元の文字を維持
- ASCIIのみの文字列
- ASCIIと漢字の混在
- 補助平面漢字を含む文字列
- 登録済みIVS / SVS
- 未登録の `BaseCharacter + VariationSelector`
- 未登録Variation SequenceではBaseCharacter単体へフォールバックしない
- Variation Selectorだけを削除しない
- 不正UTF-16を含む文字列
- 不正UTF-16部分を維持し、残りの処理を継続する
- 空文字列
- `null`


### `KanjiText.IsSupported()`

- 全文字が対象 `CharacterSet` 内
- 1文字でも対象 `CharacterSet` 外なら `false`
- ASCIIのみ
- ASCIIと漢字の混在
- 丸数字など対象文字集合外の非漢字
- 補助平面漢字
- 登録済みIVS / SVS
- 未登録Variation Sequence
- 不正UTF-16
- 空文字列
- `null`


### 置換なしの場合のstringインスタンス

`KanjiText.Replace()` で置換が1件も発生しなかった場合は、
入力された `string` インスタンスそのものを返すことをテストする。

例:

```csharp
[Fact]
public void Replace_NoReplacement_ReturnsSameInstance()
{
    var text = "高橋";

    var result = KanjiText.Replace(
        text,
        CharacterSet.JisX0208);

    Assert.Same(text, result);
}
```

これにより、置換が不要な文字列では
不要な `StringBuilder` や新しい `string` が生成されていないことを確認する。


### Build-time Generator

生成処理についても以下を検証する。

- 同じ入力データから常に同じ生成結果になる
- EntryIndexの対応が壊れていない
- Direct Lookup Tableの範囲外アクセスが発生しない
- 存在しないコードポイントは `-1`
- `CharacterSetFlags` が正しく生成される
- IVS / SVSのVariationKeyが重複しない
- VariationKeyが昇順に生成される
- MJ縮退マップ 一意な変換表の「候補なし」U+FF3Fが `0` に変換される
- AlternativeStart / AlternativeCountが正しい
- EvidenceStart / EvidenceCountが正しい
- 候補数最大8件を正しく保持できる
- Evidence数最大5件を正しく保持できる


---

## 21. ベンチマーク方針

V1ではBenchmarkDotNetを使用して、
主に検索処理と文字列置換処理を計測する。


### Lookup性能

通常UCSについて、

```text
Direct Lookup Table
```

と、

```text
ulong UnicodeKey + BinarySearch
```

の性能差を確認する。

通常UCSではDirect Lookupを採用する方針だが、
ベンチマークによって効果を確認する。


### IVS / SVS Lookup性能

IVS / SVSについて、

```text
BaseCharacter + VariationSelector
    ↓
ulong VariationKey
    ↓
Array.BinarySearch()
```

の性能を確認する。

必要であれば将来、

- 自前Binary Search
- `FrozenDictionary`
- その他の検索構造

との比較を行う。


### `KanjiText.Replace()` 性能

以下のパターンを計測する。

```text
ASCIIのみ

通常漢字のみ・置換なし

ASCII + 漢字混在・置換なし

置換1件

置換多数

補助平面漢字を含む

IVS / SVSを含む
```

特に、

```text
置換0件
置換1件
置換多数
```

を分けて測定し、
最初の置換が発生するまで `StringBuilder` を生成しない設計の効果を確認する。


### アロケーション

処理時間だけでなく、
BenchmarkDotNetのMemoryDiagnoserを使用して
メモリアロケーションも確認する。

重点的に確認する項目:

```text
置換なし
→ 追加のstring生成なし
→ StringBuilder生成なし

置換あり
→ 必要な場合のみStringBuilder生成
```

V1では、ベンチマーク結果に明確な差がない限り、
複雑な最適化や外部ライブラリへの依存は追加しない。

---

## 22. V2候補

V2では、漢字以外の互換文字も検討する。

実際のレセプト等では、

```text
① → １
② → ２
```

のような変換が必要になる場合がある。

これは、

```text
髙 → 高
```

とは性質が異なる。

前者は互換文字・業務ルール、
後者は漢字の縮退・代替関係。

またUnicode NFKCでは、

```text
① → 1
```

となるが、業務要件によっては、

```text
① → １
```

が必要になる。

したがって単純にNFKCを適用するだけでは不十分。

V2候補:

- `① → １`
- `② → ２`
- `㈱ → （株）` 等
- 単位記号
- 全角 / 半角
- 業務用途別の変換ポリシー

これらはV1には含めない。


---

# 現時点で確定している主な公開型

```text
Kanji
KanjiText
KanjiCharacter
CharacterSet
```

MJ縮退マップの代替候補・根拠情報を表す内部データ構造は、
V1のPublic APIには露出しない。

`GetVariants()` もV1では提供しない。

