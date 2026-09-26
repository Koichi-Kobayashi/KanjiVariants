# KanjiVariants

MJ縮退マップに基づいて漢字の代替候補を検索し、一意な変換先が定義されている文字を置換する .NET 6 ライブラリです。変換先は指定した文字集合で絞り込み、現在は `CharacterSet.JisX0208` を提供しています。

このライブラリは、MJ縮退マップに基づく漢字代替候補の検索と置換を目的としています。Shift-JISのバイト列を読み書きするエンコーダー／デコーダーではなく、JIS X 0201の半角カタカナなどを含むShift-JIS全体の文字検査も行いません。また、一般的な旧字体・異体字を網羅して新字体へ変換する辞書ではありません。通常の置換は、使用するMJデータに一意な変換先が定義されている場合に行います。明示的に指定した場合だけ、登録済みIVS/SVSから基底文字へのフォールバックも行えます。

## `CharacterSet.JisX0208` とは

JIS X 0208 は、日本語の文章で使う漢字や記号などを定めた文字集合です。漢字は第一水準と第二水準を含み、非漢字の記号・かな・英数字なども規定されています。このライブラリでは、そのJIS X 0208の区点位置をUnicodeに対応付けた文字を、代替候補の変換先として使います。これは文字集合への所属を調べるもので、シフトJISのバイト列を検証するものではありません。

### APIでの指定方法とIVS/SVS

この文字集合は `CharacterSet.JisX0208` としてAPIに指定します。`KanjiText.IsSupported` はASCIIを通過させ、それ以外の文字がJIS X 0208に対応するかを確認します。IVS/SVSは、漢字の字形を区別するためにVariation Selectorを後ろに付けた表記です。ライブラリは登録済みのIVS/SVSを一つの漢字表現として解析し、`GetAlternatives` や `TryGetAlternative` で代替候補を調べられます。ただしVariation Selector付きの表記自体はJIS X 0208の収録文字ではないため、`Kanji.IsSupported` と `KanjiText.IsSupported` はその表記をサポート対象外（`false`）と判定します。`KanjiText.Replace` は登録済みシーケンス全体を検索し、公式の一意な変換先が使用可能ならそれを採用します。`KanjiFallbackOptions.AllowVariationSelectorFallback` を指定した場合は、一意な変換先が使用できず、基底文字が指定集合に含まれるときに基底文字へフォールバックします。

### 対象外となる拡張文字

`CharacterSet.JisX0208` が収録文字として扱うのは、JIS X 0208に定義された文字です。CP932（Windows-31J）はJIS X 0208を基礎にした文字コードで、丸数字や一部の漢字など、JIS X 0208の範囲外の文字を独自に追加しています。また、JIS X 0213はJIS X 0208を拡張した別の規格で、追加の漢字や記号を定めています。これらの追加文字は `CharacterSet.JisX0208` に含まれず、`Kanji.IsSupported`では漢字表現を、`KanjiText.IsSupported`では文字列中の該当文字をサポート対象外と判定します。`KanjiText.Replace`も、これらの文字を自動的にJIS X 0208の文字へ変換するものではありません。MJ縮退マップに該当する登録があり、変換先がJIS X 0208に含まれる場合に限って置換します。

## 使い方

```csharp
using KanjiVariants;

var candidates = Kanji.GetAlternatives("髙", CharacterSet.JisX0208);
var converted = KanjiText.Replace("髙橋𠮷野", CharacterSet.JisX0208);
// converted == "高橋吉野"
var usable = KanjiText.IsSupported(converted, CharacterSet.JisX0208);
```

`GetAlternatives` は縮退マップ中の候補を重複なく返します。`TryGetAlternative` と `KanjiText.Replace` は、候補数ではなく公式の一意な変換表を優先します。変換先が JIS X 0208 にないときは採用しません。`Replace` は入力文字がすでに使用可能なら維持します。置換できない文字や不正な UTF-16 はそのまま残り、`IsSupported` で結果を検証できます。登録済み IVS/SVS は一つの漢字表現として解析され、未登録のシーケンスでは基底文字だけを置換しません。

### IVS/SVSのフォールバック

登録済みIVS/SVSから基底文字へのフォールバックは、オプションを指定した場合だけ行います。例えば `辻`（U+8FBB）とVariation Selector（U+E0100）の登録済みシーケンスは、基底文字の `辻` に置換できます。

```csharp
var options = KanjiFallbackOptions.AllowVariationSelectorFallback;
var result = KanjiText.Replace("辻\U000E0100", CharacterSet.JisX0208, options);
// result == "辻"
```

デフォルトの `KanjiFallbackOptions.None` ではVariation Selectorを削除しません。フォールバックでは指定されていた字形情報が失われます。公式の一意な変換先が対象集合に含まれる場合は、オプション指定時もその変換先を優先します。未登録のIVS/SVSや、基底文字が対象集合に含まれない場合はフォールバックしません。

## API の使い分け

| API | 用途 | 文字列入力で許可する内容 |
|---|---|---|
| `Kanji.GetAlternatives` | MJ縮退マップにある候補を一覧取得。オプション指定時は登録済みIVS/SVSの基底文字も候補に追加 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.TryGetAlternative` | MJ縮退マップの一意な変換先を優先。使用できない場合、オプション指定時は登録済みIVS/SVSの基底文字を取得 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.IsSupported` | 漢字表現そのものが指定集合に含まれるか確認 | 漢字一文字、または登録済み IVS/SVS |
| `KanjiText.Replace` | 変換可能な漢字を文字列内で置換。オプション指定時は登録済みIVS/SVSの基底文字へフォールバック | 任意の文字列 |
| `KanjiText.IsSupported` | 文字列全体を指定集合で検証 | 任意の文字列 |

`GetAlternatives` は候補一覧、`TryGetAlternative` は一意変換表を優先した結果です。候補一覧が1件かどうかで一意な変換先を決めるものではありません。`KanjiCharacter.Parse` は形式が不正なら `FormatException`、`TryParse` は `false` を返します。

## 一意な変換先の一覧

MJ縮退マップの一意な変換表から作成した一覧を [`data/MJUniqueAlternatives.1.2.0.csv`](data/MJUniqueAlternatives.1.2.0.csv) として同梱しています。各行はMJ文字図形名ごとの変換元表現と一意な変換先を示し、登録済みIVS/SVSも別行で掲載します。同じUnicode表現が複数行に現れる場合があります。MJ文字図形名で区別される字形ごとに変換先が異なることがあるためです。この一覧はJIS X 0208への収録可否で絞り込んでいません。変換先のUCSと、データに記録がある場合はJIS X 0213の面区点位置を掲載しています。

## 文字列処理のルール

- ASCII は使用可能として通過します。それ以外の文字は JIS X 0208 への収録を確認します。
- JIS X 0208 は IVS/SVS のシーケンスを含まないため、Variation Selector 付き表現の `IsSupported` は `false` です。
- 登録済み IVS/SVS は一つの単位として検索します。基底文字へのフォールバックはオプション指定時に限ります。未登録シーケンスは基底文字へ分解せず、そのまま残します。
- `Replace` は対象集合で使用可能な入力文字を維持します。それ以外は公式の一意な変換先を優先し、使用できない場合は明示指定されたフォールバックを試します。結果の完全な検証には `KanjiText.IsSupported` を使います。
- `Replace` は不正な UTF-16 を例外にせず保持して走査を続けます。`IsSupported` は不正な UTF-16 があれば `false` を返します。
- `Replace` と `IsSupported` に null を渡すと `ArgumentNullException` が発生します。

```csharp
if (Kanji.TryGetAlternative("髙", CharacterSet.JisX0208, out var replacement))
    Console.WriteLine(replacement); // 高

var result = KanjiText.Replace(input, CharacterSet.JisX0208);
if (!KanjiText.IsSupported(result, CharacterSet.JisX0208))
    Console.WriteLine("置換できない文字が残っています。");
```

公開メソッドの引数、戻り値、例外の説明は XML ドキュメントコメントにも記載しています。`dotnet build` で `KanjiVariants.xml` が生成されます。

## ビルドと検証

```powershell
dotnet build KanjiVariants.slnx -c Release
dotnet run --project KanjiVariants.Tests/KanjiVariants.Tests.csproj -c Release
```

テストは外部パッケージを使用しない実行形式です。生成データを更新するには、Python 3 で `python tools/generate.py` を実行します。入力を変更しない場合は同じ `GeneratedData.g.cs` が生成されます。

性能計測は別プロジェクトで実行します。`dotnet run --project KanjiVariants.Benchmarks/KanjiVariants.Benchmarks.csproj -c Release -- --filter "*"` は BenchmarkDotNet を復元し、検索・置換とメモリ割り当てを計測します。

## データと出典

`data/` に生成元を固定し、ランタイムでは `GeneratedData.g.cs` の配列だけを使います。

| データ | バージョン・出典 |
|---|---|
| MJ文字情報一覧表 | Ver.006.02、[文字情報技術促進協議会](https://moji.or.jp/mojikiban/mjlist/) |
| MJ縮退マップ、MJ縮退マップ 一意な変換表 | Ver.1.2.0、[文字情報技術促進協議会](https://moji.or.jp/mojikiban/map/) |
| Unicode IVD | 2025-07-14、[Unicode IVD](https://www.unicode.org/ivd/data/2025-07-14/) |
| Unicode Standardized Variants | Unicode 18.0.0、[Unicode Character Database](https://www.unicode.org/Public/UCD/latest/ucd/StandardizedVariants.txt) |
| JIS X 0208 の Unicode 対応 | Python の `euc_jp` デコーダーで区点 1–94 を走査して生成。Windows CP932 の拡張文字は含めない。 |

MJデータの著作権者は独立行政法人情報処理推進機構（IPA）です。MJ文字情報一覧表とMJ縮退マップは [CC BY-SA 2.1 JP](https://creativecommons.org/licenses/by-sa/2.1/jp/) により提供されています。Unicode のデータは [Unicode Terms of Use](https://www.unicode.org/terms_of_use.html) に従います。

## 免責事項

本ライブラリおよび同梱データは現状のまま提供されます。本ライブラリまたは同梱データの利用により発生したいかなる損害についても、作者は一切責任を負いません。ライセンス条件の異なる同梱データについては、`LICENSE-NOTICES.md` もご確認ください。
