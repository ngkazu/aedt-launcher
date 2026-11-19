# aedt-launcher 改善項目

対象: `aedt-launcher/` (WPF / .NET 10, `net10.0-windows`)

## 1. 起動時に実行ファイル自身のパスを解析対象にしている

- 場所: `MainWindow.xaml.cs` の `Window_Loaded`
- 内容: `checkFile(args)` に `args[0]`（実行ファイル自身のパス）も渡している。
- 影響: 解析対象外のパスを処理しており、無駄な処理になっている。
- 終了フラグ: [ ]

## 2. 複数ファイル指定時に最後の1件しか表示されない

- 場所: `MainWindow.xaml.cs` の `checkFile(string[])`
- 内容: ループ内で毎回テキストボックスを上書きしているため、最後のファイルの結果だけが残る。
- 影響: 複数ファイルをドロップ/引数指定したときに、先頭のファイルの情報が見えない。
- 終了フラグ: [ ]

## 3. 不正なファイルで例外が未処理のまま落ちる可能性

- 場所: `FileClass.cs` の `checkSAversion`、`checkAEDTversion`、`checkSIversion`、`extrctSAversion`
- 内容: gzip でない `.tgz`、壊れた `.aedt` / `.siw` などで例外が発生しても捕捉していない。
- 影響: ドロップ時にアプリケーションが異常終了する可能性がある。
- 終了フラグ: [ ]

## 4. Launch の失敗・未設定が無表示

- 場所: `FileClass.cs` の `launchApplication`
- 内容: `catch {}` が空で、`App.config` に設定キーがない場合や起動失敗時に何も通知されない。
- 補足: README には「キーがない場合は何も起動せずエラーも表示しない」と記載されている。表示方針を変更する場合は README も併せて更新が必要。
- 終了フラグ: [ ]

## 5. パス区切りのハードコード

- 場所: `FileClass.cs` の `checkSAversion`
- 内容: `Path.GetDirectoryName(fi) + "\\" + te.Name.Split('/')[0]` のように `"\\"` を直接連結している。
- 影響: `Path.Combine` を使うと安全に組み立てられる。
- 終了フラグ: [ ]

## 6. 未使用コード・命名・リソース管理の整理

- 未使用の変数・型: `MainWindow.xaml.cs` の `list`（`MyFileList`）、`App.xaml.cs` の未使用 `using`
- typo / 命名: `extrctSAversion`（extract）、`get_filetye`（filetype）
- リソース: `launchApplication` の `Process` が Dispose されていない
- 補足: 公開メソッド名を変更するとすべての呼び出し元の修正が必要になる。
- 終了フラグ: [ ]

## 7. 解析ロジックの単体テストがない

- 対象: AEDT / SIwave / Icepak のバージョン判定（`checkAEDTversion`、`checkSIversion`、`checkSAdirectory`、`ParseIcepakVersion`）
- 内容: テストプロジェクトが存在しないため、解析処理の回帰を検出する手段がない。
- 補足: テストプロジェクトの追加は依存関係の変更を伴うため、実施前に承認が必要。
- 終了フラグ: [ ]

## 8. aedtz ファイル（7-Zip 形式）への対応

- 対象: 拡張子 `.aedtz` の追加
- 内容: `.aedtz` は 7-Zip 形式のアーカイブのため、展開してからバージョンを判定する必要がある。
- 方針: テンポラリへの展開は行わず、メモリ上で 7z アーカイブを読み、バージョン判定に必要なエントリの内容だけを文字列として取り出す。
- ライブラリ: 現在の SharpZipLib は 7z 非対応のため、7z をストリームから読める SharpCompress（MIT ライセンス）を追加する。`SevenZipArchive` でストリームを開き、必要なエントリのストリームだけを読む。依存関係の変更を伴うため承認が必要。
- 実装方針:
  - 既存の `checkAEDTversion` / `checkSIversion` は `File.ReadLines` 前提のため、`TextReader` を受け取る形に分けて、ファイル・メモリ両方から同じ判定ロジックを使う。
  - 全体を展開せず、対象エントリ（`.aedt` / `.siw` 相当）だけを `StreamReader` で読む。
  - AES 暗号化されたエントリは読めないため、その場合は「暗号化されているため判定不可」と表示する。
- 要確認: 展開後のファイル構成（どのエントリからバージョンを読むか）。サンプル `data/Graphics_Card_Test.aedtz`（pyaedt の `tests/system/**/*.aedtz` も参考）で確認する。
- 補足: README の「Supported Files」表の更新が必要。
- 終了フラグ: [ ]

## 9. siwz ファイル（7-Zip 形式）への対応

- 対象: 拡張子 `.siwz` の追加
- 内容: `.siwz` も 7-Zip 形式のため、項目 8 と同じメモリ上の読み取り処理を共通化する。
- サンプル: `data/BD-SAMPLE.DSGN_GD6.siwz`
- 要確認: 展開後のエントリ構成と、バージョンの取得元（現在の `.siw` の `DB_VERSION` 判定を流用できるか）。
- 補足: README の「Supported Files」表の更新が必要。
- 終了フラグ: [ ]

## 10. 環境変数から AEDT の実行パスを取得

- 内容: 現在は `App.config` の `appSettings` のみから実行パスを取得している。環境変数からも取得できるようにする。
- 調査結果（このPC）:
  - システム環境変数 `ANSYSEM_ROOT<数値>` が存在する（数値はバージョンに対応）。
    - `ANSYSEM_ROOT242` = `C:\Program Files\AnsysEM\v242\Win64`
    - `ANSYSEM_ROOT251` = `C:\Program Files\ANSYS Inc\v251\AnsysEM`
    - `ANSYSEM_ROOT252` = `C:\Program Files\ANSYS Inc\v252\AnsysEM`
    - `ANSYSEM_ROOT261` = `C:\Program Files\ANSYS Inc\v261\AnsysEM`
    - `ANSYSEM_ROOT271` = `C:\Program Files\ANSYS Inc\v271\AnsysEM`
  - C# からの取得: `Environment.GetEnvironmentVariables()` で列挙可能（システム・ユーザー両方を含む）。名前は正規表現 `^ANSYSEM_ROOT\d+$` で絞り込める。
  - 注意: アプリ起動後にシステム側で変更された変数はプロセスに反映されない。反映させる場合はレジストリ `HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Environment` を直接読む必要がある。
  - 未確認: 各フォルダに `ansysedt.exe` が存在するか。
- 要確認:
  - 環境変数 `ANSYSEM_ROOT<数値>` を使う場合の、実行ファイルへの組み立て方（ルート直下か `Win64` 配下か）
  - `App.config` と環境変数の優先順位
- 補足: 項目 4（未設定時の表示）と関連する。README の「Executable Configuration」の更新が必要。
- 終了フラグ: [ ]

## 11. Ctrl+Q でアプリが終了しない

- 場所: `MainWindow.xaml` の `File > Quit`
- 内容: メニューの `InputGestureText="Ctrl+Q"` は表示のみで、実際のショートカット（`KeyBinding`）が定義されていないため、Ctrl+Q を押してもアプリが終了しない。
- 対応案: `Window` に `KeyBinding`（Ctrl+Q → `MenuItem_Click` 相当の終了処理）を追加する。
- 補足: 既存の終了処理（`app_Exit`）を再利用できるため、変更は小さい。
- 終了フラグ: [ ]

## 12. アプリアイコンがさみしい

- 内容: アプリアイコンの見た目が寂しい印象になっている。
- 原因: `aedt-launcher.csproj` で `ApplicationIcon` を指定していないため、デフォルトアイコンが使われている。
- 作り方:
  1. 256×256px 以上の透過PNGを作成する（Inkscape / GIMP / Figma などで作成）。
  2. `magick input.png -define icon:auto-resize=256,48,32,24,16 app.ico` で複数サイズの ICO に変換し、`Images\` に置く。
  3. csproj に `<ApplicationIcon>Images\app.ico</ApplicationIcon>` を追加する。
- 補足: 具体的なデザイン方針は未定。
- 終了フラグ: [ ]

## 方針・未決事項のまとめ（8・9・10）

### 方針

- **aedtz / siwz（項目 8・9）**: テンポラリへ展開せず、メモリ上で 7z を読み、判定に必要なエントリだけを文字列として取り出す。
- **ライブラリ**: SharpCompress（MIT）を追加する想定。SharpZipLib は 7z 非対応。
  - SharpCompress は 7z に加えて TAR / GZip（`.tar.gz` を含む）にも対応している。既存の `.tzr` / `.tgz` 処理（SharpZipLib）を SharpCompress に統一できるが、既存処理の変更になるため別項目として扱う。
- **判定ロジックの共通化**: `checkAEDTversion` / `checkSIversion` を `TextReader` 入力に分け、ファイルとメモリで同じ判定を使う。
- **暗号化**: AES 暗号化されたエントリは「暗号化されているため判定不可」と表示する。

### 未決・確認事項

- [ ] 承認: SharpCompress の NuGet 追加（依存関係の変更）
- [ ] 確認: aedtz の展開後エントリ構成（どのエントリからバージョンを読むか）。サンプル `data/Graphics_Card_Test.aedtz`
- [ ] 確認: siwz の展開後エントリ構成と、バージョン取得元（`DB_VERSION` を流用できるか）。サンプル `data/BD-SAMPLE.DSGN_GD6.siwz`
- [ ] 決定: `ANSYSEM_ROOT<数値>` を使うか、使う場合の実行ファイルの組み立て方
- [ ] 決定: `App.config` と環境変数の優先順位
- [ ] 決定: 項目 4 の未設定時表示方針（README の記載変更を含む）

### 実装前に必要な作業

- README の「Supported Files」「Executable Configuration」の更新案を提示し、承認を得る。
- 項目 7（単体テスト）を実施する場合は、テストプロジェクト追加の承認を得る。
