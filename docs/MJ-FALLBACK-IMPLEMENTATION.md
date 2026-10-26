# MJ縮退逆引きAPIの実装・検証結果

検証日: 2026-10-25（日本時間）。作業ブランチ: `feature/mj-fallback-reverse`。
開始HEAD: `b4bab26c457ab796c8b01ed02093034c24da3896`。開始時に未コミット変更なし。
コミット・Push・公開・バージョン更新は実施していない。

## 1. 公開APIと例

```csharp
var sources = MjFallback.GetSources("高");
var same = MjFallback.GetSources(KanjiCharacter.Parse("高"));
foreach (var source in sources)
    foreach (var evidence in source.Evidence)
        Console.WriteLine($"{evidence.Source} → {evidence.Target}: {evidence.MjGlyphName}, {evidence.RelationKind}");
```

`IReadOnlyList<MjFallbackSource>`を返す。`Character`は元表現、`Evidence`は複数根拠。
一般的な異体字検索ではなく、指定した縮退先に直接結び付く元表現の検索。
正規化、推移展開、JIS集合での絞り込み、自動置換は行わない。

文字列入力は既存の`KanjiCharacter.Parse`と同じ規約。nullはArgumentNullException、空文字・複数文字・不正UTF-16・未登録VSはFormatException。
有効だが該当なし、登録済みVS付き入力、既定値のKanjiCharacterは空。原典の縮退先はVSなし。

## 2. 新規・変更ファイル

- 新規公開API: `KanjiVariants/Mj/MjFallback.cs`、`MjFallbackSource.cs`、`MjFallbackEvidence.cs`、`MjFallbackRelationKind.cs`。
- 新規データ: `KanjiVariants/Generated/GeneratedMjFallbackData.g.cs`。
- 新規生成器・テスト: `tools/generate_mj_fallback.py`、`tools/test_generate_mj_fallback.py`、`KanjiVariants.Tests/MjFallbackTests.cs`。
- 変更: `KanjiVariants/Core/KanjiCharacter.cs`（検証済み生成表向けの内部ファクトリーのみ）、`tools/check_generated.py`、`tools/test_check_generated.py`、`README.md`、`LICENSE-NOTICES.md`。
- 本報告書。計測用プローブ・パッケージ・生測定値はGit管理外の`.local/mj-fallback-audit/`。

## 3. 逆索引の生成

専用生成器から既存`generate.py`のMJ一覧読取・スカラー解析・登録VS照合を再利用。
既存MJ一覧006.02、縮退マップ1.2.0、既存の固定IVD/SVSを入力とする。外部データ追加なし。
実装UCS・互換漢字を元表現とし、それらのない行だけ対応UCSを補助表現として採用。登録済みIVS/SVSも採用。
自己候補を除き、縮退先→元表現→根拠集合を生成する。5分類を保持し、参考情報やU+FF3Fの候補なし番兵は採用しない。

索引は8,821検索キー、39,852候補、62,481根拠参照。候補は基底スカラー・VS順、根拠はMJ番号・分類等の順で固定。
既存スカラー索引の先勝ち集約を利用せず原典全行から作るため、以前の調査でDLLの既存配列を逆集計した39,661組とは範囲が異なる。

数値配列を埋め込み、文字列メタデータは単一プールとUTF-16オフセットで保持。数千の文字列を初回に全件解析・オブジェクト化しない。
検索は二分探索し、該当キーの不変結果だけを遅延構築・共有。初回の同時呼び出しはロックとVolatileで安全に公開し、ヒット時はロック・候補再構築なし。
キャッシュは検索済みキーについて保持し、最大8,821キー。既存APIから新索引は参照しない。

再生成: `python tools/generate_mj_fallback.py`。一致検証: 同コマンドの`--check`。
既存の生成済みファイルを管理する方式に合わせ、Pythonの生成工程と.NETコンパイルは分離。通常のdotnet build時にPythonを要求しない。
`tools/check_generated.py`へ登録し、既存CIの全生成検査に含めた。

## 4. 根拠情報

`MjFallbackEvidence`はSource→Targetという原典上の縮退方向、MJ文字図形名、五分類、元表現のMJ列由来、JIS面区点、通知種別、ホップ数、付記、表、順位を保持。
同一候補の異なるMJ・分類・詳細は残す。完全に同じ根拠の重複だけを統合。
`SourceRepresentationKind`で実装UCSと補助的な対応UCSを区別する。後者を実装済み字形とみなさない。
ホップ数は原典の情報であり、新たな推移検索はしない。
MJ加工データのCC BY-SA 2.1 JPとUnicode通知を維持し、通知対象に新索引を追加した。

## 5. 実行例

| 検索 | 実測した候補の例 | 備考 |
|---|---|---|
| 高 | 髙 | MJ028902の包摂・統合、戸籍親字・正字の2根拠 |
| 斎 | 齋 | 一意変換表だけでなく一般の縮退関係を利用 |
| 崎 | 﨑 | 同上 |
| 辻 | U+8FBB U+E0102、U+8FBB U+E0103 | 独立した全IVS列挙ではない |
| 欄 | U+6B04 U+FE00 | SVS候補も保持 |
| U+323AF | 空 | 解析可能・該当関係なし |

「高」全結果は亯、髙、𠅠、𠆪、𦕺、𦴒。すべてを単純な交換可能字体として提示してはならない。

## 6. テスト

- Debug/Release: solution build成功、各0警告・0エラー。
- 両構成でライブラリ586件、CLI121件成功。ライブラリは既存566件＋追加20ケース。
- Python: 既存＋新規17件成功。
- 固定原典から9生成器＋既存MJ派生CSVを非更新再生成し、一致。
- 全39,852候補・62,481根拠のUnicode往復、元方向、自己除外を検査。
- 複数根拠、複数MJの統合、5分類、詳細メタデータ、自己除外、非推移、安定順、補助平面、IVS/SVS、不正入力、不変性、既存API回帰を検査。
- 独立プロセスから新API未呼出しの状態で32並行検索し、同一の完成済み結果を共有することを検査。
- `git diff --check`成功。

全生成検査にはpdfplumber 0.11.9を含む同梱Pythonを使用。標準Pythonには既存のPDF生成器の依存がなかったため切り替えた。

## 7. 性能・配布サイズ

Windows x64、.NET SDK 10.0.402、ホスト10.0.13、Release。
HEADの変更前DLLと変更後DLLを別出力へ配置。同じ計測プログラムを独立プロセスで各5回起動し、中央値を採用。
階層JITを無効化。初回はJIT・必要な静的初期化・結果構築を含み、OSプロセス起動は含まない。
連続検索はKanjiCharacter引数、1万回の予熱後100万回。単純プローブであり統計的な性能保証ではない。

| 測定 | 変更前の既存API | 変更後の既存API | 新API |
|---|---:|---:|---:|
| 高の初回検索（ms） | 14.3580 | 13.7925 | 16.4513 |
| 連続検索（ns/回） | 71.86 | 71.33 | 77.61 |
| 連続検索割当（B/回） | 96 | 96 | 0 |
| 初回割当（B） | 1,793,480 | 1,793,480 | 3,281,264 |
| 初回後の保持ヒープ増分（B） | 1,793,288 | 1,793,288 | 3,280,800 |

既存APIは`Kanji.GetAlternatives("高", JisX0208)`、新APIは`MjFallback.GetSources("高")`。
両者は異なる検索で、新APIには変更前の対照APIがない。ヒープはGC後の差分で、プロセス全体のRSSではない。
新APIの文字列オーバーロードは登録済みVSの検証等を含む場合があり、上の連続検索値は型付き引数のみ。

| 配布物 | 変更前 | 変更後 | 増分 |
|---|---:|---:|---:|
| Release DLL | 6,166,528 B | 9,508,352 B | 3,341,824 B |
| ローカル生成nupkg | 1,286,408 B | 2,061,831 B | 775,423 B |

nupkgは双方同じ作業環境のdotnet packによる比較。公開済み署名付きパッケージとの直接比較ではない。
既存APIのみを使用した際の保持メモリは増えていないが、ライブラリ全体の配布サイズは増える。

## 8. NativeAOT・WASM

- win-x64 NativeAOT: publish成功。5検索例・32並行初期化と全39,852候補・62,481根拠の検査成功。
- NativeAOT単回の参考値: 初回2.9085ms、保持ヒープ増分3,162,144 B、連続70.30ns/回、連続割当0 B/回。JIT版の5回中央値とは測定条件が異なる。
- win-x64 self-contained＋PublishTrimmed: publishと検索例・並行初期化・全候補検査の実行成功。
- Blazor WebAssembly Release: トリミング込みpublish成功。ローカルで配信し、ブラウザ上で高・斎・崎・辻・欄の5例すべてPASS。
- WASM AOT: 実行不可。RunAOTCompilation=trueはNETSDK1147（wasm-tools未導入）。新しいワークロードのインストールは実施せず、未確認とする。
- macOS/Linux向けNativeAOTおよび他アーキテクチャは未確認。

## 9. 既存APIとの互換性

既存のGeneratedData.g.cs、MJ派生CSV、GetAlternatives、TryGetAlternative、IsSupportedの処理、パッケージTFMとVersionは変更なし。
KanjiCharacterの変更は内部ファクトリー追加のみ。既存の解析・検証経路は維持。
既存テストと全生成物照合により、従来の縮退データ・集合判定の回帰を検査した。

## 10. 制約・残る課題

全関連文字を網羅するAPIではない。JIS集合への適合と、元表現の関係は別。
データはMJ原典由来の固定範囲で、フォントの表示対応や氏名の置換可否は保証しない。
キャッシュは検索したキーの結果を保持する。上記メモリ値は高を一回検索した場合であり、全キー検索時の最大保持量ではない。
配布サイズ増加は実測のとおり。さらに圧縮する場合は起動時の展開コストとの比較が必要。
IMEでは初回を打鍵処理の外で予熱し、関係種別で候補を選別し、既存候補と優先順位を保つことを推奨。
