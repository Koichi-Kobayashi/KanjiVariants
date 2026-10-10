# KanjiVariants.Cli

教育漢字・常用漢字をチェックする.NET Toolです。UTF-8のTXTファイル全体、またはSRT字幕本文を診断します。.NET 10ランタイムが必要です。

## インストール

NuGet.orgへの公開後は、次のコマンドでインストールできます。

```powershell
dotnet tool install --global KanjiVariants.Cli
```

公開前のローカルパッケージは、リポジトリ直下から次のように検証できます。

```powershell
dotnet pack KanjiVariants.Cli/KanjiVariants.Cli.csproj -c Release -o ./artifacts/nuget
dotnet tool install KanjiVariants.Cli --tool-path ./artifacts/tool-test --add-source ./artifacts/nuget --version 1.2.0
./artifacts/tool-test/kanjivariants.exe check sample.txt --grade 3
```

## 使用方法

```powershell
kanjivariants check sample.txt --grade 3
kanjivariants check sample.txt --grade 3 --unique
kanjivariants check sample.txt --grade 3 --json
kanjivariants joyo sample.txt
kanjivariants joyo subtitle.srt --json
kanjivariants check subtitle.srt --grade 3 --unique --json
```

`check` は指定した小学学年までの教育漢字、`joyo` は常用漢字表の本表字を確認します。旧字体・異体字・IVS/SVSを自動変換しません。`--unique` は同じ判定の文字を集約し、`--json` はJSONだけを標準出力へ返します。SRTでは字幕番号・時間・本文内の位置も取得できます。

入力はBOMあり／なしのUTF-8だけに対応します。不正UTF-8は読込エラーです。改行を保持し、正規化や文字置換は行いません。

終了コードは、0：診断対象の問題なし、1：未習漢字または非常用漢字あり、2：引数・読込・SRT構文・入力異常です。引数・読込・構文エラーは標準エラーへ出力します。

## 更新・削除

```powershell
dotnet tool update --global KanjiVariants.Cli
dotnet tool uninstall --global KanjiVariants.Cli
```

## ライセンスと出典

CLIのコードはMIT Licenseです。同梱のKanjiVariantsには外部公開データ由来の表も含まれます。各データの出典・利用条件は同梱の[LICENSE-NOTICES.md](LICENSE-NOTICES.md)、詳細なデータバージョン・生成方法は[リポジトリのREADME](https://github.com/Koichi-Kobayashi/KanjiVariants/blob/main/README.md)を参照してください。
