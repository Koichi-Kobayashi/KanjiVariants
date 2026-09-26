# KanjiVariants

指定された文字集合で使用できる漢字の代替候補を探し、MJ縮退マップの一意な変換表に基づいて文字列を置換する .NET 6 ライブラリです。現時点の文字集合は `CharacterSet.JisX0208` です。

```csharp
using KanjiVariants;

var candidates = Kanji.GetAlternatives("髙", CharacterSet.JisX0208);
var converted = KanjiText.Replace("髙橋𠮷野", CharacterSet.JisX0208);
// converted == "高橋吉野"
var usable = KanjiText.IsSupported(converted, CharacterSet.JisX0208);
```

`GetAlternatives` は縮退マップ中の候補を重複なく返します。`TryGetAlternative` と `KanjiText.Replace` は、候補数ではなく公式の一意な変換表を使います。変換先が JIS X 0208 にないときは採用しません。置換できない文字や不正な UTF-16 はそのまま残り、`IsSupported` で結果を検証できます。登録済み IVS/SVS は一つの漢字表現として解析され、未登録のシーケンスでは基底文字だけを置換しません。

## API の使い分け

| API | 用途 | 文字列入力で許可する内容 |
|---|---|---|
| `Kanji.GetAlternatives` | MJ縮退マップにある候補のうち、指定集合に含まれる候補を一覧取得 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.TryGetAlternative` | MJ縮退マップの一意な変換表から一つの変換先を取得 | 漢字一文字、または登録済み IVS/SVS |
| `Kanji.IsSupported` | 漢字表現そのものが指定集合に含まれるか確認 | 漢字一文字、または登録済み IVS/SVS |
| `KanjiText.Replace` | 変換可能な漢字を文字列内で置換 | 任意の文字列 |
| `KanjiText.IsSupported` | 文字列全体を指定集合で検証 | 任意の文字列 |

`GetAlternatives` は候補一覧、`TryGetAlternative` は一意変換表による結果です。候補一覧が1件かどうかで一意な変換先を決めるものではありません。`KanjiCharacter.Parse` は形式が不正なら `FormatException`、`TryParse` は `false` を返します。

## 文字列処理のルール

- ASCII は使用可能として通過します。それ以外の文字は JIS X 0208 への収録を確認します。
- JIS X 0208 は IVS/SVS のシーケンスを含まないため、Variation Selector 付き表現の `IsSupported` は `false` です。
- 登録済み IVS/SVS は一つの単位として検索します。未登録シーケンスは基底文字へ分解せず、そのまま残します。
- `Replace` は置換可能な文字だけを書き換え、置換先が対象集合外なら入力を維持します。結果の完全な検証には `KanjiText.IsSupported` を使います。
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
