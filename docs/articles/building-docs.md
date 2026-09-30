# ドキュメントの生成と閲覧

## 前提

.NET SDKと、初回のLocal Tool・プロジェクトの復元に必要なネットワーク接続を用意してください。ライブラリの対象は.NET 6ですが、Docfx 2.81.0の実行には.NET 8以降が必要です。グローバルインストールは不要です。

リポジトリルートで実行します。

```powershell
dotnet tool restore
dotnet build KanjiVariants.slnx -c Release
dotnet docfx docs/docfx.json
```

`docs/docfx.json` のmetadataがライブラリのcsprojを読み込み、public APIとXMLコメントから `docs/api/` にYAMLを生成します。その後、ガイドと合わせて `docs/_site/` にHTMLを生成します。既存の設計メモはサイト生成対象に含めていません。

## ローカル閲覧

```powershell
dotnet docfx serve docs/_site --port 8080
```

ブラウザで [http://localhost:8080](http://localhost:8080) を開きます。終了はCtrl+Cです。生成直後にサーバーを起動する場合は次のコマンドも利用できます。

```powershell
dotnet docfx docs/docfx.json --serve --port 8080
```

生成YAML・HTMLはGit管理せず、NuGetパッケージにも含めません。編集するのはガイドと公開APIのXMLコメントです。生成されたAPIのtocは自動生成を使用し、機能別の入口は `api/index.md` にまとめています。

## 公開

公開ドキュメント: [https://koichi-kobayashi.github.io/KanjiVariants/](https://koichi-kobayashi.github.io/KanjiVariants/)

`.github/workflows/docs.yml` がmainへの対象パスのpushで、復元・Releaseビルド・全テスト・Docfx生成・リンク確認を実行します。`docs/_site/` を公式Pages artifactとしてアップロードし、別のdeployジョブが `github-pages` 環境へ公開します。生成物のコミットは不要です。SDKは.NET 8と.NET 10を用意します。Solutionにはライブラリとテストだけが含まれ、Benchmarkはビルド・実行しません。

GitHubリポジトリの **Settings → Pages → Build and deployment → Source** で **GitHub Actions** を選択してください。初回公開前は公開URLへアクセスできない場合があります。`github-pages` 環境の保護設定を使用している場合は、mainからのデプロイを許可してください。追加のPATやSecretsは不要です。

手動実行する場合は **Actions → Publish documentation → Run workflow** でmainを選択します。main以外のブランチではbuildのみを実行し、公開は行いません。

Docfxが生成する内部リンク・CSS・JavaScriptは相対パスで、Project Pagesの `/KanjiVariants/` 配下でも解決します。CIで全HTMLのローカルリンクとアセットをこの配下で検証します。DocfxのbaseUrl設定は追加せず、ローカル閲覧も同じ構成を使用します。

Docfxは開発用Local Tool依存です。Docfx本体をライブラリやサイトの配布物へコピーしないため、この導入によるLICENSE-NOTICES.mdの追記は行っていません。文字データの利用条件は引き続き同ファイルを参照してください。

CLI仕様: [Docfx公式ドキュメント](https://dotnet.github.io/docfx/docs/basic-concepts.html)
