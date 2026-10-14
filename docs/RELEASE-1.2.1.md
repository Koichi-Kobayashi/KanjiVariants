# NuGet 1.2.1 リリースソースと検証記録

履歴: 2026-10-13の監査対応確定・未コミット候補の検証を記録し、2026-10-14にリリースソースのレビューと最終候補の検証手順を整理した。実際の公開は別工程。

## 監査対応の確定

コミット: `f3b607ba796a7ae4eb5b6306bf6c7dc96b2b3d9f`

メッセージ: `JIS X 0208の生成元とライセンス表示・ビルド再現性を改善`

親: `37e2aabe5313fd8fa4eaf0d86437967e0ee56449`。監査対応のみ18ファイルを明示してステージし、ステージ差分を確認、diff --check成功後にコミットした。原典の固定SHAをWindows checkoutでも保つため、.gitattributesの-text指定を含めた。キャッシュ・一時パッケージ・生ログは除外。

コミット対象:

- `.gitattributes`
- `.github/workflows/build.yml`
- `Directory.Build.props`
- `KanjiVariants/Generated/GeneratedData.g.cs`
- `KanjiVariants/Generated/GeneratedJinmeiyoKanjiData.g.cs`
- `KanjiVariants/Generated/GeneratedMjCharacterData.g.cs`
- `KanjiVariants/KanjiVariants.csproj`
- `LICENSE-NOTICES.md`
- `README.md`
- `data/JIS/JIS0208.TXT`
- `data/JIS/Unicode-LICENSE.txt`
- `docs/JIS-X0208-PROVENANCE-AUDIT.md`
- `tools/VerifyBuildArtifacts/Program.cs`
- `tools/VerifyBuildArtifacts/VerifyBuildArtifacts.csproj`
- `tools/generate.py`
- `tools/generate_jinmeiyo.py`
- `tools/generate_mj_character.py`
- `tools/test_generate.py`

## リリースソースの変更

- Directory.Build.props: ライブラリのVersionのみ1.2.1。独立CLIのKanjiVariantsCliVersionは1.2.0を維持。
- KanjiVariants/KanjiVariants.csproj: パッチのPackageReleaseNotesを更新。net6.0、PackageId、Repository URL、ライセンスファイル、README設定を維持。
- README.md: ライブラリ導入例1.2.1と公開後に利用する説明。CLI版やMJ原典/派生CSVの1.2.0表記は維持。
- docs/RELEASE-1.2.1.md: 本報告書。

## リリースノート

JIS X 0208の生成元をSHA-256固定のUnicode JIS0208.TXT Table 2.0へ変更し、原典・Unicode License V3全文を同梱しました。1区32点のU+005C→U+FF3C互換処理により、既存の所属判定と全生成データを維持しています。MJ加工データのCC BY-SA 2.1 JP適用範囲を明確化し、SDKのソース位置正規化により開発PC固有のPDB絶対パスを除去しました。対応.NETターゲットと公開APIは変更していません。

## 履歴: 2026-10-13の未コミット候補検証

検証用（正式リリース用ではない）: `.local/release-1.2.1/pack/KanjiVariants.1.2.1.nupkg`。Git管理外の専用出力。既存公開1.2.0を上書きしていない。

- Debug/Release solutionビルド成功。本体net6.0、tests net8.0、CLI/CLI tests net10.0を維持。
- 本体566件、CLI121件: 成功、失敗/skip0。Python13件成功。
- 全8生成器・MJ派生CSV一致。生成データ本体に変更なし。
- 公開NuGet1.2.0実DLLとの公開API469項目比較: 差分0。
- 全65,536バイトBmpCharacterSetFlags: 差分0。JIS0208以外のビットも不変。
- 全8,836区点、割当6,879、未割当1,957、旧集合差分0をPythonテストで再確認。
- Debug/Release本体DLL/PDB: 2 CodeView、108 PDB文書、SourceLinkキー/HTTPS値検査成功。ローカル絶対パス0。
- パッケージ10エントリー全体をUTF-8/UTF-16LE/BEで検査: ローカル絶対パス0。
- nuspec Version=1.2.1、Repository URL/commit、README/licenseメタデータを確認。
- README.md、LICENSE-NOTICES.md、data/JIS/JIS0208.TXT、data/JIS/Unicode-LICENSE.txt、DLLの同梱バイト列は元ファイルと一致。
- MJ通知のCC BY-SA 2.1 JP表示を維持。独自コードと加工データの適用範囲を分離。
- 原典SHA-256: `1c571870457f19c97720631fa83ee491549a96ba1436da1296786a67d8632e87`。
- 候補nupkg SHA-256: `ca0afe291f10e24422049b5414f4be7e833fc20773a7bbca4de9e08762dff852`。

生ログ・JSON検証結果: `.local/release-1.2.1/`。前工程の全ターゲット検査は監査報告書参照。本候補のsolution外Benchmarksは再ビルド対象としていない（監査コミット前に両構成を検証済み）。

## 履歴: 未コミット候補のSourceLink

候補のRepository commitとSourceLinkは監査対応HEAD `f3b607ba796a7ae4eb5b6306bf6c7dc96b2b3d9f` を指す。実SourceLink:

`https://raw.githubusercontent.com/Koichi-Kobayashi/KanjiVariants/f3b607ba796a7ae4eb5b6306bf6c7dc96b2b3d9f/*`

C#ソース51ファイルが同コミットのGit blobと改行正規化後に一致することをローカル検証した。しかし、バージョン/パッケージメタデータ/READMEは未コミットであり、候補を最終リリースコミットから生成した正式版として扱わない。未PushのためGitHubでのソース取得・URL内容照合は未確認。標準nupkgにPDBは含まれず、同じビルドの外部portable PDBを検査した。DLLのCodeViewは仮想PDBパスを保持する。

## 最終候補の検証手順（リリースコミット後に実行）

この文書を含む4ファイルをリリースコミットに確定し、その新HEADからGit管理外の専用ディレクトリへclean checkoutする。コミット自身のハッシュやビルド後に判明するパッケージハッシュは、この文書へ事前に埋め込まない。最終候補の結果・SHA-256・生ログ・検証器はGit管理外の記録に保存する。上記の過去の候補ハッシュを最終候補へ流用しない。

1. library Version=1.2.1、CLI=1.2.0、既存TFMを確認。Debug/Release build、本体566・CLI121・Python13テスト、全8生成器とMJ派生CSVチェックを再実行。
2. 新規出力へRelease pack。nuspec/Repository commit/DLLのSourceLinkが新HEADを指すことを確認する。
3. PEReader/portable PDBでCodeView/PDB/SourceLinkの仮想パスとローカル絶対パス0を検査。PDB文書チェックサムを新HEADのGit blobへ直接照合する。SDK生成文書はGit管理ソースと区別し、生成時の内容・Embedded Sourceと照合する。
4. 公開済み1.2.0実DLLと469 API/65,536 BMPフラグを比較。全8,836区点と全生成データを確認し、可能ならPlaygroundの一時Checksを新DLLで実行する。本番依存・lockは変更しない。
5. pack内のREADME/通知/Unicode原典/許諾全文/DLLをclean checkoutのソース・出力とbyte照合し、全エントリーをUTF-8/UTF-16でローカルパス検査する。
6. 最終結果は別記録へ保存。問題があれば公開候補として承認せず、追加コミット/amendは別途判断する。

## CI事前レビューと公開前の残作業

build.ymlはmain Push（対象path変更時）/手動起動で生成器・本体/CLI tests・DLL/PDBパス検査を行う。NuGet pack/パッケージ監査、PDBチェックサム対Git blob照合は現行CIにないため、今回のローカル最終検証で実施する。独立CLIのみの変更がpush path filterにない点も今後の改善候補。

現行2 workflowにはNuGet.orgへの公開処理・タグ/Releaseイベントを契機とする公開はない。ただしdocs.ymlはmain Pushでドキュメントビルド後にGitHub Pagesをdeployする。今回はPushもActions起動も行わない。将来Pushする前にドキュメント自動公開の扱いを確認する必要がある。

未PushのSourceLink URLは外部取得成功とは扱わず、今回はローカルGitコミットとの整合性まで検証する。ソース公開後の実URL取得・ソース照合、実GitHub runnerのCI、必要な署名/公開先/認証の確認は別工程。別OSのbyte単位ビルド再現性も未検証。

Playgroundへの採用は別工程。現行1.2.0依存・publicationApproved=false・A1は維持し、新通知/Webcilパス/2,333 Checksを移行時に検証する。過去1.2.0のPython API由来データの権利評価が遡及解決したとは扱わない。
