> 以下の初回監査記録を保持しています。2026-10-13の修正後の状態・検証・A1の区別は末尾「修正後の検証」を参照してください。

# JIS X 0208生成元・許諾・正確性の独立監査

監査日: 2026-10-13（日本時間）。調査のみ。生成器・生成データ・DLL・NuGet版・公開承認・チェックを変更していない。コミット/amend/Push/デプロイ/問い合わせ送信なし。

## 1. 結論とA1

**生成経路・再配布対象・正確性は特定できた。A1の全面解除は現時点で保留する。著作権侵害を確認したという結論ではない。** 当時のPythonディストリビューション/版は未特定で、API出力から作る所属ビットに対する権利評価と、原データの許諾・通知の対応を既存の曖昧な通知だけで最終承認していない。

重要な確認済み事項:

- 公開版の処理は8,836区点をPython euc_jpで走査し、6,879 Unicode文字の**所属集合**を作る。区点→Unicodeの対応対、Python Cコード、内部圧縮表の配列/索引構造はNuGetへコピーしない。
- NuGet 1.2.0 DLLの実フラグ65,536 bytesと公開版C#ソースは全bytes一致。最新ソースともJIS所属が一致。
- 独立したUnicode JIS0208.TXT原表は6,879割当。CPython2.4/3.13.0の**内部生表**はこれと全件一致。
- 通常のeuc_jp出力は、1区32点を原表のU+005CからU+FF3Cへ変える。CPython C実装とREADMEに明示された互換処理で、表の誤転記ではない。
- 原表にこの1点の明示的な互換方針を適用して作る所属集合は、公開NuGet DLLのJISビットと完全一致する。将来の原典切替を**所属結果の変更なし**で行える根拠が得られた。
- 現NuGet/PlaygroundにはUnicode License V3全文がバイト一致で同梱済み。JIS0208.TXTの版固定出典・加工と互換方針の説明は未収録。PSF全文がないことだけで違反とは判断しない。

推奨は**案B: 公式Unicode原表を固定し、1区32点の互換方針を明示して現行所属を維持する**こと。今回は提案のみ。案Aも可能性があるが、現データの権利評価/通知を根拠付きで確定する追加工程が必要。完全な昔の実行環境記録がないこと自体をライセンス違反の証拠にはしない。

## 2. 調査対象を区別する

| 対象 | 固定識別 |
|---|---|
| NuGet KanjiVariants 1.2.0 | 実nupkg/nuspec、lib/net6.0/KanjiVariants.dll |
| 公開版ソース | nuspecのrepository commit `983fa8e6b98b7af0c503c686560b62c0778c9738` |
| 本体最新ローカルHEAD | `37e2aabe5313fd8fa4eaf0d86437967e0ee56449`、開始時clean |
| Playground | `9be45b7c41b998a3d40b614da0ebe57a1495f9fc`。既存未追跡docs/PREPUBLICATION-AUDIT.mdを保持 |
| 今回の比較用Python | bundled CPython3.12.14、MSC v.1944 x64 Windows、sys.versionを記録。**当時の生成Pythonではない** |
| 読んだCPython版固定ソース | v3.13.0。歴史比較はv2.4と導入/再編/生成器追加commit |

Gitから公開版のtools/generate.py、GeneratedData.g.cs、build workflowを直接取得。最新HEADとの対象3ファイル（生成器・生成データ・LICENSE-NOTICES.md）のGit差分は0だったが、NuGetが最新HEADを使用したという意味ではない。DLLも別に実測した。

調査用ソース・比較スクリプト・生結果はPlaygroundのGit管理外 `artifacts/jis-x0208-audit/`。本体の追跡ファイル変更は本報告書のみ。完全再生成は元リポジトリへ書き出すため実施せず、対象処理の独立比較とDLL照合を行った。

## 3. 公開版の具体的な生成・配布経路

原ソース: [tools/generate.py@公開commit](https://github.com/Koichi-Kobayashi/KanjiVariants/blob/983fa8e6b98b7af0c503c686560b62c0778c9738/tools/generate.py)。行203–214がJIS集合、80–100が所属ビット、266–285が生成配列の組立。

1. ku/tenそれぞれ1～94（range(1,95)）を走査、計94×94=8,836。
2. 2 bytes `(ku+0xA0,ten+0xA0)`、各A1～FEを `decode('euc_jp')`（デフォルトstrict）へ渡す。
3. UnicodeDecodeErrorなら未割当としてskip。復号文字数1の結果だけordをsetに追加。NFC/NFKCや異体字正規化はしない。
4. BMP65,536 code pointについて集合にあればJIS_X_0208=1を設定。MJ由来JIS X 0213各面のbitを同じbyteへORする。
5. [GeneratedData.g.cs@公開commit](https://github.com/Koichi-Kobayashi/KanjiVariants/blob/983fa8e6b98b7af0c503c686560b62c0778c9738/KanjiVariants/Generated/GeneratedData.g.cs)の `internal static readonly byte[] BmpCharacterSetFlags`（行7038から）へ出力。補助平面配列にJIS X 0208 bitは設定しない。
6. C#配列初期値をDLLへコンパイル。実際のDLLをReflectionでロードし当該fieldを読み、全65,536 bytesが公開ソースと一致した。

DLL SHA-256: `a6e0172bc75f6c5aaf5c3d26bd935a5b854092f7074e23d0c220324dd526aaab`。
複合フラグarray SHA-256: `e13c299a846ae1c670b3e5b656f682ba77ba8b39dc1191f7f0bee52c1ac4a710`。後者にはMJ由来X0213 bitも含むので「Python対応表全体のhash」と呼ばない。

再配布対象はUnicodeごとの所属ビットと、同じ生成ファイルにある別出典のMJ等データ。JIS区点順・元表のUnicode名/コメント・Shift-JIS列・CPythonの索引や穴埋め構造は残らない。Python実行ファイル/codec CコードはNuGetやPlaygroundに配信されない。API出力を多数収集した集合であり、コードをコピーしないことだけでデータの権利問題が消えるとはいわない。

呼出元は [Core/Lookup.cs](https://github.com/Koichi-Kobayashi/KanjiVariants/blob/983fa8e6b98b7af0c503c686560b62c0778c9738/KanjiVariants/Core/Lookup.cs)行31–46、Kanji.IsSupported/TryGetAlternative、KanjiText.IsSupported/Replace、KanjiDiagnostics等。KanjiText行77–87はASCIIを別の高速経路で常に許容する。規格そのもののASCII所属やShift-JISエンコード可否の表ではない。

## 4. 生成時Pythonを特定できたか

**特定できていない。** Git履歴上generate.pyは最初の `5592233`（2026-09-27）で導入され、その後の生成器履歴も確認した。requirementsやworkflowはPython版を固定せず、ubuntu-latestのPython3でvenvを作る。version表示stepはあるが、これはCIの再生成チェック環境であって、最初の生成/ローカルNuGet製造環境と同一とは限らない。

公開commitのCIはBuild DLL run `37783209171` と文書run `37783209257` が2026-10-08に成功している。公開APIでmetadataを確認したが、logs取得は403で、Python版のログ本文を読めなかった。認証なしで成功に置き換えていない。現在CPython3.12.14で一致すること、2.4/3.13生表が一致することは、過去の環境の一意特定ではない。

## 5. CPython euc_jpの実装と来歴

版固定 [CPython v3.13.0](https://github.com/python/cpython/tree/v3.13.0)を取得し、次を確認。

| ファイル/位置 | 確認した内容 |
|---|---|
| Lib/encodings/euc_jp.py:7–13 | _codecs_jp.getcodec('euc_jp')に接続。Python wrapperだけで数表を持たない |
| Modules/cjkcodecs/_codecs_jp.c:382–440 | C decoder。ASCII、SS2半角、SS3 JIS0212、JIS0208の分岐。今回の2 bytes A1–FEはJIS0208分岐のみ |
| 同:427–433 | STRICT_BUILDでない通常実装はA1C0をU+FF3Cに置き換えてから、残りはjisx0208表を参照 |
| Modules/cjkcodecs/mappings_jp.h:2/534 | __jisx0208_decmap 6,956 slots（穴のU=未定義を含む）+256 index。6,879割当。**slots数と文字数を混同しない** |
| Modules/cjkcodecs/README:euc-jp節 | 全角reverse solidusの互換処理、単バイトyen/overline encodeの例外を説明。後2つは今回の区点走査範囲外 |
| Tools/unicode/genmap_japanese.py:24,76,84,171–178 | UnicodeのJIS0208.TXTを読込。第2列JIS、第3列Unicodeからデコード表を生成。CP932/JIS0212/JIS0213は別入力・別表 |
| Tools/unicode/genmap_support.py | 行列をC配列/索引へ整形。文字コード対応の意味を変える処理と配列配置を区別 |
| Lib/test/test_codecmaps_jp.py | euc_jp互換mapのテストをcp932/shift-jis/JIS0213と別に用意 |

歴史は公式Gitと[導入Issue873597](https://bugs.python.org/issue873597)で確認した。

- 2004-01-17 [3e2a306](https://github.com/python/cpython/commit/3e2a30692085d32ac63f72b35da39158a471fc68): CJK codec初回導入。旧Modules/cjkcodecs/_euc_jp.cの作者はHye-Shik Chang。旧READMEは2004-01-17時点のCJKCodecs由来、mapping headerは外部distributionの事前生成物と説明する。
- 2004-07-18 [2bb146f](https://github.com/python/cpython/commit/2bb146f2f4fd52b03cfa7ae739adb35d2b9f5421): CJKCodecs1.1取り込みと現在の_codecs_jp.c/mappings_jp.h配置への再編。これは初回導入ではない。
- v2.4のmappings_jp.hを実解析し、JIS0208の6,879対応が現在取得したUnicode原表と一致。
- 2020-04-29 [113feb3](https://github.com/python/cpython/commit/113feb3ec2b08948a381175d33b6ff308d24fceb): bpo-40328、元作者/改修者のmap generatorをTools/unicodeへ追加。現生成器がUnicode原表を指定する証拠。2004年に同じURL/ライセンス版を取得したことを証明するものではない。

CPython配布に収録するコードの許諾は当該版LICENSEを確認した。旧独立CJKCodecs distribution全体のLICENSE/個別contributor agreement原本まで確認していない。コード作者名だけからBSD/GPL等を推測しない。JIS0208対応の現在の生成器と生表に対してはUnicodeまで具体的に追跡できた。

## 6. 原典とライセンス

### CPythonを再配布する場合

[v3.13.0 LICENSE](https://github.com/python/cpython/blob/v3.13.0/LICENSE)行73以降のPSF License Version2を確認。Pythonまたはその派生版に許諾・PSF著作権を保持、変更した派生版を配布する場合の変更概要、免責等がある。商用利用/改変/配布は許されるが条件を満たす必要がある。過去のBeOpen/CNRI/CWIの履歴部分もあり、当該版の実配布通知を一般名だけで代用しない。codecs wrapper/C実装/内部tableレイアウトをコピーするなら、Pythonコードと元データ両方の条件を検討する。

### 独立Unicode原典

[JIS0208.TXT](https://www.unicode.org/Public/MAPPINGS/OBSOLETE/EASTASIA/JIS/JIS0208.TXT): 公開元Unicode, Inc.、名称JIS X0208(1990) to Unicode、Unicode1.1、Table2.0、2011-10-14・header更新2015-12-02。©2015 Unicode, Inc.、terms_of_useへの参照を含む。OBSOLETEは現行推奨の万能表という意味ではなく、legacy mapping原典として固定して使う。

[Unicode Terms of Use](https://www.unicode.org/copyright.html)のDefinitionsはPublic配下のcomputer data fileをData Filesとし、Permissions/Licenses節は特記がないData Filesに[Unicode License V3](https://www.unicode.org/license.txt)を適用する。JIS0208.TXTには別の制限許諾は見つからず、この原典を新たに取得して加工する案の配布条件は特定できる。規格書PDF/字形画像には別の条件があり、同一視しない。

Unicode License V3はデータの利用/複製/改変/配布/販売を許し、著作権・許諾通知をコピーまたは関連文書に保持する条件、免責と名義利用の制限がある。コード全体のcopyleft条件はない。原表headerの©2015表示も出典に保存する案が明確。現在の©1991–2026の包括表示があることと、JIS表の由来/旧headerを明示済みであることを混同しない。

**ライセンスの3ケース**:

| ケース | 今回の該当/評価 |
|---|---|
| Pythonソースを再配布 | 該当しない。C/Pythonコード・codec索引レイアウトを配布する証拠なし。仮にコピーするなら版固定Python LICENSEと個別第三者条件を保持 |
| 内部表の複製・加工 | raw headerを直接読む生成器ではない。とはいえ網羅的API収集が原表の保護対象を加工したものかは権利評価を伴い、API経由というだけで否定しない |
| 変換APIで得た結果の再利用 | **実際の処理はこのケース**。独自生成器で文字集合のbitを作る。符号対応/集合という技術的事実と、原コード/表の表現・選択配置の複製を区別。PSFが全outputへ自動適用される条文を確認していない |

一致は正確性・来歴を裏付けるが、それだけで当時取得したPython/元表のすべての権利条件を遡及確定しない。逆にライセンス名・Python版が未記録というだけで侵害としない。必要ならPSFのlicensing担当/Unicodeへ「この網羅的API収集からの所属bit配布に追加通知が必要か」を具体的に照会する。今回は送信していない。

## 7. 全区点比較・正確性

同じA1～FEの2-byte範囲で独立検証した。Unicode原典の4列はShift-JIS、JIS、Unicode、名前で、**JIS第2列を使用**した。列の取り違えやCP932入力はない。

| 比較 | 結果 |
|---|---|
| 走査区点 | 8,836 |
| 復号割当/未割当 | 6,879 / 1,957 |
| 復号からのUnicode集合 | 6,879、同一Unicodeへの複数対応0、多文字復号0、補助平面0 |
| Unicode原表 | 6,879、割当位置集合は一致 |
| CPython3.12.14 APIとUnicode | 1区点相違、その他の全割当/未割当一致 |
| CPython2.4/3.13.0生表とUnicode | 各6,879全対応一致（codec例外は別） |
| 公開ソース/最新ソース/API出力のJIS所属集合 | 各6,879、差分0 |
| NuGet DLLと公開C#の複合フラグ | 65,536 bytes完全一致 |
| Unicode原表+明示した1区32点方針とDLL JIS bit | 65,536 bitの判定結果で差分0 |

唯一の区点差分:

| 区点 | JIS bytes | EUC-JP | CPython通常出力 | Unicode原表 | 原因 |
|---|---|---|---|---|---|
| 1-32 | 0x2140 | A1C0 | U+FF3C FULLWIDTH REVERSE SOLIDUS | U+005C REVERSE SOLIDUS | _codecs_jp.c:427–433の通常ビルド互換処理 |

Unicode原表には可能な対応の一例で、製品ごとの方針は異なるとの説明がある。Pythonの差分を「絶対に正しい唯一のUnicode対応」や「誤り」と断定しない。現KanjiVariantsの名称は、正確には**JIS X0208(1990)の割当集合を通常CPython euc_jp互換のUnicode方針で表した所属判定**を意味する。

代表点も比較: 1-33 U+301C、1-34 U+2016、1-61 U+2212、1-81 U+00A2、1-82 U+00A3、2-44 U+00ACはいずれも原表と一致。U+FF5E/2225/FF0D/FFE0/FFE1/FFE2等のWindows側置換を持ち込まない。16-1はU+4E9C、84-6はU+7199、85-1/94-94は未割当。

JIS X0212は8F付き3 bytesであり今回対象外。JIS X0213は別codec/別MJデータ。JIS Romanのyen/overline/ASCIIのcodec規則は今回の2-byte走査とは別。CP932/Windows-31Jの拡張区点を混ぜていない。文字集合への適合と、EUC-JP/Shift-JISへのencode可能性は別で、KanjiTextのASCII例外もある。

**互換性**: 純粋なUnicode原表へ例外なしで切り替えると、bitはU+FF3Cの削除とU+005Cの追加という2 code pointで変わる。公開KanjiText.IsSupported('\\')はASCII経路で既にtrueなので、その利用例は変化しないが、'＼'はtrue→falseとなり変換・候補選択・診断へ影響し得る。今回DLLで'\\'と'＼'はtrue、'〜'はtrue、'～'はfalseと実測。原表+明示的互換方針ならJIS bit差分0で維持できる。

## 8. 既存通知の充足

本体ルートに独立LICENSEはなく、LICENSE-NOTICES.mdがMIT全文と外部データ条件をまとめた実許諾文書。ファイル名がLICENSEでないこと自体は不足ではない。本体の独立THIRD-PARTY-NOTICESディレクトリはなく、同文書に集約する。

公開版/最新のLICENSE-NOTICES.mdに対象差分0。実NuGet通知とPlaygroundのTHIRD-PARTY-NOTICES/KanjiVariants-1.2.0.mdもバイト一致。通知SHA-256 `f44f7db48acd15378b7648c7c6205f65fbec884f23479d1d31fcec78d719cf80`。

| 項目 | 状態 |
|---|---|
| Python euc_jpから生成した旨 | 既存通知にある |
| JIS0208.TXT名/版/URL/hash | 未記載 |
| 1区32点の方針と加工 | 未記載 |
| Unicode License V3全文 | 実nupkg内から抽出した本文が今回の公式全文と改行正規化後一致 |
| Unicode copyright | ©1991–2026の包括表示あり。原表©2015の出典headerは未明示 |
| PSF全文 | 未収録。Pythonコード配布がないため、それだけで義務違反とは認定しない |
| 本体生成表のMIT/外部データ区分 | プログラムコードMITからGeneratedData.g.csの外部表を除外する旨あり |

追加する案: LICENSE-NOTICES.mdのJIS0208節へ原典Table2.0/URL/©2015/sha、所属bitへの加工、1区32点の方針、Unicode License V3への接続を明記。許諾全文は既存を再利用してよいが、その対象にJIS表も明示する。PSFコード/tableレイアウトの派生に当たると評価した場合だけ、正しい版のPSF/元コード通知・変更概要を追加する。推測した「Python使用必須通知」を作らない。

案Bでは固定原典をdata/JISへ保存してheaderと許諾原文を保持し、README/生成ヘッダー/NOTICEで加工と互換方針を説明する。新NuGetに同梱し、Playgroundの同梱通知もその版へ更新・publishで照合する。今回はいずれも修正していない。

## 9. 解決案の比較

| 項目 | 案A・現行維持と通知 | 案B・固定Unicode原典+互換方針 | 案C・追加確認 |
|---|---|---|---|
| 許諾の確実性 | API結果/所属bitの権利評価と必要通知の確認を要する | 新たに取得するData FilesのV3適用を原典で説明できる。過去の配布の遡及承認とは別 | 権利者回答/履歴資料まで待つ |
| 信頼性 | CPython経路・原典・正確性は確認済み、当時Python版不明 | Table2.0固定hash、個人環境とcodec更新に依存しない | 当時環境が必ず復元できるとは限らない |
| データ差分 | 0 | 互換方針ありで所属0。例外なしなら2所属bit差分 | 調査だけなら0 |
| API互換性 | 維持 | 提案した互換方針で維持。純原表案はFF3C動作変更 | 維持 |
| 実装 | 原則通知/設計説明だけ | generatorを固定原典parserへ変更、1区32点を仕様化、比較/固定hash検査 | 問い合わせ/証跡整理 |
| NuGet再リリース | 同梱通知を改訂するなら既存1.2.0を上書きせず修正版。外部補足だけで済むかは評価が必要 | 原則新パッケージ。版番号は今回変更しない | 今回不要 |
| Playground | 適法根拠と通知を満たした場合A1解除を検討 | 旧依存を使い続ければ新generatorだけでA1解除とはしない。新版採用/通知の別作業が必要 | A1保留 |

案Bを推奨する理由は「元表も自由だから過去のPython出力も無条件に自由」とするためではなく、将来の入力・許諾・加工方針を直接固定し、同じ機能を検証可能にするため。機能を変えない表現方法での解消候補を示せたが、正式採用と通知変更は未実施。

## 10. A1解除条件への回答

| 条件 | 今回の状態 |
|---|---|
| 具体的な生成経路 | 確認済み。元環境の版までではない |
| 配布データの特定 | 実DLLを含め確認済み |
| 必要な許諾を説明 | CPython再配布/Unicode Data Files/変換API出力を区別して説明。現出力の保護対象性と元環境全条件の最終確定は残る |
| 必要な表示を特定 | Unicode原典と加工方針の明示案を特定。PSF追加の要否は無条件に決めない |
| 未確認と既知問題の分離 | 当時版不明・権利評価未了と、正確性/通知明確化を区別。侵害確認なし |

したがって**A1を今回の調査だけで解決済みにしない**。少なくとも以下のいずれかを別工程で満たす。

1. 案Aとして現出力の再配布根拠・通知要否を確認し、必要な対象/出典通知を整備したうえで公開版1.2.0利用の扱いを決定する。
2. 案Bとして固定原典の再生成・通知・修正版NuGet・Playground採用を実施し、旧版に関する判断とは分けて新配布物のA1を閉じる。

## 11. 未確認事項と追加調査

- NuGet製造者の当時のPython版、インストーラー/実行ログ、生成コマンド、codec binary hash。作者の保存ログ/CI version stepへのアクセスで確認する。CI版だけを元生成版としない。
- 2004年独立CJKCodecs distributionの原LICENSE/全入力履歴、原著者のgrant/PSF CLA個票は未確認。現在の公式CPython全体LICENSEやUnicode原典だけで未確認個票を推測しない。
- 網羅的API走査から所属bitへ加工した出力の保護対象性/追加許諾要否。PSF licensing/Unicodeへ処理図・6,879所属bit・コード非配布・1点互換処理を提示して照会する案。送信していない。
- JIS規格票そのものの全版の規範的Unicode対応を検証したのではない。原表はJIS X0208(1990)のpossible mapping。比較の全件一致をすべての製品/規格版への保証に広げない。
- 今回は機能変更なし。ライブラリ全テスト/新NuGet製造/Pages配信は実施しない。独立8,836区点、raw table、DLL65,536 bytes、代表公開APIだけを検証した。

## 12. Playground報告書への追記案（未適用）

> A1継続調査: 公開NuGet1.2.0対応commitの生成器、DLL所属フラグ65,536 bytes、CPython内部表、Unicode JIS0208.TXTを照合。6,879割当/1,957未割当、原表との差分は1区32点のFF3C互換処理だけ。再配布物は区点対ではなく所属bit。生成経路/正確性は確認済み。当時Python版と現出力の権利/通知の最終評価は未了のためA1解除保留、侵害確認ではない。固定Unicode原典+明示的互換方針は所属差分0で実現可能。正式採用・通知/パッケージ更新は別作業。publicationApproved=falseを維持。

Playground側の既存監査報告書は変更せず、上記の追記案のみ提示した。

## 付録・検証証跡

Git管理外の `artifacts/jis-x0208-audit/` に sources.json（取得URL/SHA）、comparison.json/all-kuten.json（8,836件）、raw-table-comparison.json（2.4/3.13）、nuget-dll-inspection.json/nuget-dll-flags.bin/dll-source-comparison.json、policy-comparison.json、版固定source/原典/license、公式history JSON、CI metadata/logアクセス結果を保存。現在の実行環境と当時の特定を分けて記録している。

主要原典hash:

| 原典 | SHA-256 |
|---|---|
| Unicode JIS0208.TXT（Table2.0） | 1c571870457f19c97720631fa83ee491549a96ba1436da1296786a67d8632e87 |
| CPython3.13.0 mappings_jp.h | 09012ff9eb963073d42a8bce375c05484219537caa0cc2aa74baa63d5f3f1658 |
| CPython3.13.0 LICENSE | 78b12c3a81360b357002334f0e70ea0e92eebf7a9b358805c03c48484945f3bb |
| 取得したUnicode License V3全文 | e7a93b009565cfce55919a381437ac4db883e9da2126fa28b91d12732bc53d96 |

HTTP fallbackや取得不可を成功にしていない。公式raw sourceはweb表示で取得できない場合、許可された読み取りコマンドで直接取得。履歴照会の旧仮定パス_japanese.cは0件、実commitで確認した旧_euc_jp.cへ追跡し直した。Python2.4の現行生成器パスは404で、その版に現配置があるとは扱わない。CIログ403は未確認のまま。

## 2026-10-13: 修正後の検証（初回監査と区別）

### 採用した修正

- 固定Unicode JIS0208.TXT Table 2.0をdata/JISへ原文バイトのまま保存。監査済みSHA-256は不変。Unicode License V3全文を保存しnupkgへ原典と共に同梱。
- tools/generate.pyは原典第2/第3列を読み、hash・区点/Unicode妥当性・重複・6,879割当/1,957未割当/8,836全区点を検査。1区32点をU+FF3Cへ明示変更。Python euc_jp APIを生成処理から除去。
- MJ加工データの原著作者IPA、原典の版、加工者Koichi Kobayashi、加工内容/適用範囲、CC BY-SA 2.1 JP URIと継承をREADME/通知/対象3生成ヘッダーで明記。独自C#コードMIT/Unicode等と区別。既存許諾全文3ブロックは改行正規化後の本文を保ち、削除・改変なし。
- Directory.Build.propsでDeterministic / DeterministicSourcePaths / ContinuousIntegrationBuildを有効化。SDKのSourceRoot正規化からPathMapとSourceLinkを揃えて生成。CodeView/PDB文書とSourceLinkキーは `/_/`（依存rootはSDKの `/_n/`）へ仮想化し、PDBを削除しない。対応TFM/公開APIは変更なし。

手動PathMapだけの初回実験はCodeViewの仮想化には成功したが、PDB内SourceLinkのキーに物理rootが残ったため採用しなかった。SDKの統合正規化へ変更し、再ビルド・再検査した。検査器の最初のdrive検出はhttpsの末尾s:/を誤検出したので境界を正し、公開URLとローカルパスを区別した。solution外のBenchmarks旧出力を一度検査して失敗したが、明示的なDebug/Release再ビルド後に成功した。これら初期失敗を最終成功に含めない。

### 確定した検証結果

| 検証 | 最終結果 |
|---|---|
| 固定原典 | SHA-256 1c571870457f19c97720631fa83ee491549a96ba1436da1296786a67d8632e87、原文/ヘッダー不変 |
| 区点/所属 | 8,836評価、割当6,879、未割当1,957、所属6,879、旧集合差分0 |
| 65,536 byte BMP複合フラグ | 公開NuGet1.2.0実DLLと修正DLLで完全一致、MJ由来JIS0213 bitも不変 |
| 全8生成C#データ | namespace以降のコード・全配列完全一致。対象3ファイルのヘッダーのみ変更 |
| MJ派生CSV | 36,646行の内容と元バイト列を保持 |
| Pythonテスト | 13件成功。旧DLLのBMPフラグ固定hash・全区点・原典改変拒否を含む |
| 全8生成器チェック | 成功、派生CSVも一致 |
| 全.NETテスト | 本体566 + CLI121成功、失敗/skip0 |
| Debug / Release | solutionとsolution外Benchmarksが各0警告・0エラー。library net6、tests net8、CLI/CLI tests/Benchmarks net10を維持 |
| 公開API比較 | 実NuGet1.2.0と候補DLL、469公開型/member記録、差分0 |
| CodeView / portable PDB | 本体/CLI Debug・Releaseとnet8テストRelease: 5 CodeView・164文書成功。Benchmarks両構成2 CodeView・22文書成功。net8 Debug/CLI testsも仮想メタデータを確認 |
| 配布DLLローカル絶対パス | 0。SourceLinkのHTTPS公開URL・公表著作権者名は除外して評価 |
| ローカルpack | .local/prepublication-fixes/pack/KanjiVariants.1.2.0.nupkg成功（正式版変更なし、公開物の上書きなし） |
| pack同梱 | LICENSE-NOTICES/README/固定原典/Unicode全文/候補DLLを元ファイルとバイト照合。全展開ファイルUTF-8/UTF-16のlocal path検査0件 |
| Playground互換 | 本番のcsproj/lock/NuGet cacheを変更せず、Git管理外の一時ハーネスで候補DLLに対して既存2,333 Checks成功 |
| Git | 未コミット・ステージなし。初回監査報告書を保持/追記。バージョン/TFM/依存の更新なし |

候補Release DLL SHA-256: `87c981357377b4c7cea8e724a95e7c36ec6f6d0c64172b54780a08c783db466a`。

ローカルpackは検査用の同版名であり、公開済み1.2.0を差し替えるものではない。配布用の正式修正版番号は別途相談して決める。NuGet.org/本番feedへ送信しない。

### 検証器と再実行

CIのbuild.ymlに既存CLI testsとtools/VerifyBuildArtifactsを追加。PEReaderでCodeView、MetadataReaderでportable PDB全文書を確認し、UTF-8/UTF-16のパスも検査する。旧公開DLLは非仮想CodeViewで拒否される。ローカルでCI同様の決定論的設定と手順は検証したが、実GitHub runnerでの実行・別OSでのDLL hash一致は未検証。PDBはSourceLinkを維持するが、この未コミット候補の公開URLは既存HEADを示すため、正式版のソース一致は将来のコミット/リリース工程で再確認する。

```text
python -m unittest discover -s tools -p 'test_*.py' -v
python tools/check_generated.py
dotnet build KanjiVariants.slnx -c Debug
dotnet build KanjiVariants.slnx -c Release --no-restore
dotnet test KanjiVariants.slnx -c Release --no-build
dotnet run --project tools/VerifyBuildArtifacts -c Release -- KanjiVariants/bin/Release/net6.0/KanjiVariants.dll KanjiVariants/bin/Release/net6.0/KanjiVariants.pdb
dotnet pack KanjiVariants/KanjiVariants.csproj -c Release --no-build -o .local/prepublication-fixes/pack
```

今回はPython3.12.14（pdfplumber0.11.9/pdfminer.six20251230）、Windows/.NET SDK10.0.401で実行。生ログは本体.local/prepublication-fixes、旧データ比較はPlayground artifacts/kanjivariants-fixesに保存。生成処理は固定bytesと整数処理に変更したが、別Python版での実行そのものは未検証。一時Checksの最初のXMLエンコード名utf8はMSBuildが拒否したためutf-8へ修正してから実行成功した。

### Playgroundへ移行する別工程

1. 正式修正版番号・公開ソースcommit/来歴を決め、通知と候補packを再確認して新パッケージを用意する。旧1.2.0を上書きしない。
2. Playground app/checksのPackageReferenceとpackages.lock.jsonを正式修正版へ更新する（今回未変更）。THIRD-PARTY-NOTICES/KanjiVariantsの同梱通知・csprojリンク・LICENSE-NOTICES/ライセンス画面の版表記を更新する。
3. Debug/Release/全2,333 Checks/publishを再実行し、WebcilのKanjiVariants.wasm（br/gzを含む）を展開してCodeView由来のlocal path0と新通知の同梱を確認する。Playground独自assemblyに残るPDBパスは本体の設定だけでは直らず、別作業が必要。
4. source/採用依存/publishの一致を示してPlayground側A1を再評価する。publicationApproved=falseを維持し、他の公開前課題を独立して判断する。

候補ビルドには新固定原典の生成経路・許諾・通知を整備し、本体側3項目は検証済み。ただし過去のPython出力の権利評価を遡及解決したとはしない。**現行Playgroundは依然として旧NuGet1.2.0なので、その公開前監査A1を今回解除していない。** 新パッケージ正式採用後の配布物レビューが残る。

### 変更ファイル

Directory.Build.props、KanjiVariants.csproj、tools/generate.py/generate_jinmeiyo.py/generate_mj_character.py、3生成C#のヘッダー、LICENSE-NOTICES.md、README.md、tools/test_generate.py、.github/workflows/build.yml。新規data/JIS/JIS0208.TXT・Unicode-LICENSE.txt、tools/VerifyBuildArtifactsのcsproj/Program.cs。既存未追跡の本監査報告書は保持し本節を追記。生成データ本体・CSV・公開APIコード・NuGet版・Playgroundには変更なし。

最終の全対象検査: 5プロジェクト（本体/CLI/本体tests/CLI tests/Benchmarks）DebugとReleaseのDLL/PDB計20ファイル、10 CodeView、228 PDB文書を検証し、SourceLinkのvirtual key/HTTPS値も確認。全対象のローカル絶対パス0件。文書更新後の最終packもREADME/通知/原典/許諾/DLLのbyte一致・10展開ファイルの検査0件を再確認。

コミット前の改行監査: core.autocrlf=trueのWindows checkoutでも原典hashを保持するため、.gitattributesでdata/JIS/JIS0208.TXTとUnicode-LICENSE.txtを-textに指定。既存の固定ICU/UCMと同じ方針。原典の変更はなく、Gitのcheckoutフィルター出力も固定SHAと一致することを確認。
