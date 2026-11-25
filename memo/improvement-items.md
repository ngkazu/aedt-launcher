# aedt-launcher 改善項目

対象: `aedt-launcher/` (WPF / .NET 10, `net10.0-windows`)

## 複数ファイル指定時に最後の1件しか表示されない (#3)

- 場所: `MainWindow.xaml.cs` の `checkFile(string[])`
- 内容: ループ内で毎回テキストボックスを上書きしているため、最後のファイルの結果だけが残る。
- 影響: 複数ファイルをドロップ/引数指定したときに、先頭のファイルの情報が見えない。
- 終了フラグ: [ ]

## Launch の失敗・未設定が無表示 (#6)

- 場所: `FileClass.cs` の `launchApplication`
- 内容: `catch {}` が空で、`App.config` に設定キーがない場合や起動失敗時に何も通知されない。
- 補足: README には「キーがない場合は何も起動せずエラーも表示しない」と記載されている。表示方針を変更する場合は README も併せて更新が必要。
- 終了フラグ: [ ]

## aedtz ファイル（7-Zip 形式）への対応 (#7)

- 対象: 拡張子 `.aedtz` の追加
- 内容: `.aedtz` は 7-Zip 形式のアーカイブのため、展開してからバージョンを判定する必要がある。
- 方針: テンポラリへの展開は行わず、メモリ上で 7z アーカイブを読み、バージョン判定に必要なエントリの内容だけを文字列として取り出す。
- ライブラリ: 現在の SharpZipLib は 7z 非対応のため、7z をストリームから読める SharpCompress（MIT ライセンス）を追加する。`SevenZipArchive` でストリームを開き、必要なエントリのストリームだけを読む。依存関係の変更を伴うため承認が必要。
- 実装方針:
  - 既存の `CheckAEDTversion` / `CheckSIversion` は `File.ReadLines` 前提のため、`TextReader` を受け取る形に分けて、ファイル・メモリ両方から同じ判定ロジックを使う。
  - 全体を展開せず、対象エントリ（`.aedt` / `.siw` 相当）だけを `StreamReader` で読む。
  - AES 暗号化されたエントリは読めないため、その場合は「暗号化されているため判定不可」と表示する。
- 要確認: 展開後のファイル構成（どのエントリからバージョンを読むか）。サンプル `aedt-launcher.Tests/TestData/Graphics_Card_Test.aedtz`（pyaedt の `tests/system/**/*.aedtz` も参考）で確認する。
- 補足: README の「Supported Files」表の更新が必要。
- 終了フラグ: [ ]

## siwz ファイル（7-Zip 形式）への対応 (#8)

- 対象: 拡張子 `.siwz` の追加
- 内容: `.siwz` も 7-Zip 形式のため、#7 と同じメモリ上の読み取り処理を共通化する。
- サンプル: `aedt-launcher.Tests/TestData/BD-SAMPLE.DSGN_GD6.siwz`
- 要確認: 展開後のエントリ構成と、バージョンの取得元（現在の `.siw` の `DB_VERSION` 判定を流用できるか）。
- 補足: README の「Supported Files」表の更新が必要。
- 終了フラグ: [ ]

## 環境変数から AEDT の実行パスを取得 (#9)

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
- 補足: #6（未設定時の表示）と関連する。README の「Executable Configuration」の更新が必要。
- 終了フラグ: [ ]

## アプリアイコンがさみしい (#10)

- 内容: アプリアイコンの見た目が寂しい印象になっている。
- 原因: `aedt-launcher.csproj` で `ApplicationIcon` を指定していないため、デフォルトアイコンが使われている。
- 作り方:
  1. 256×256px 以上の透過PNGを作成する（Inkscape / GIMP / Figma などで作成）。
  2. `magick input.png -define icon:auto-resize=256,48,32,24,16 app.ico` で複数サイズの ICO に変換し、`Images\` に置く。
  3. csproj に `<ApplicationIcon>Images\app.ico</ApplicationIcon>` を追加する。
- 補足: 具体的なデザイン方針は未定。
- 終了フラグ: [ ]

## 展開先の上書き確認がなく、展開後のフィードバックもない (#18)

- 場所: `FileClass.cs` の `ExtractSAversion`、`MainWindow.xaml.cs` の `button_Extract_Click`
- 内容:
  - 展開先は元ファイルと同じフォルダで、既存のファイル・フォルダがあっても確認せず上書きする。
  - 展開が成功したか、どこに展開されたかが画面に表示されない。
  - Extract ボタンは押すたびに展開を実行する。
- 対応案: 展開先が既に存在する場合は確認する。展開完了後にパスを表示し、展開済みならボタンを無効にする。
- 終了フラグ: [ ]

## App.config のバージョンキーとインストール先の対応を確認していない (#19)

- 場所: [App.config](d:/+Projects/repos/aedt-launcher/aedt-launcher/App.config)
- 内容:
  - `AEDT:2019.1` が `AnsysEM19.3` フォルダを指している。`AEDT:2018.x` のキーは `AnsysEM19.x` フォルダを指しており、キーの年とフォルダ名の対応が揃っていない。
  - インストール先パスがハードコードされているため、環境によっては起動できない。
- 対応案: ファイル内のバージョン表記（`begin 'Desktop'` の値など）と各キーの対応を、実際の AEDT / Icepak のバージョンで確認する。
- 補足: #9 の環境変数対応を行う場合は、この確認結果を前提にする。
- 終了フラグ: [ ]

## 方針・未決事項のまとめ（#7・#8・#9）

### 方針

- **aedtz / siwz（#7・#8）**: テンポラリへ展開せず、メモリ上で 7z を読み、判定に必要なエントリだけを文字列として取り出す。
- **ライブラリ**: SharpCompress（MIT）を追加する想定。SharpZipLib は 7z 非対応。
  - SharpCompress は 7z に加えて TAR / GZip（`.tar.gz` を含む）にも対応している。既存の `.tzr` / `.tgz` 処理（SharpZipLib）を SharpCompress に統一できるが、既存処理の変更になるため別項目として扱う。
- **判定ロジックの共通化**: `CheckAEDTversion` / `CheckSIversion` を `TextReader` 入力に分け、ファイルとメモリで同じ判定を使う。
- **暗号化**: AES 暗号化されたエントリは「暗号化されているため判定不可」と表示する。

### 未決・確認事項

- [ ] 承認: SharpCompress の NuGet 追加（依存関係の変更）
- [ ] 確認: aedtz の展開後エントリ構成（どのエントリからバージョンを読むか）。サンプル `aedt-launcher.Tests/TestData/Graphics_Card_Test.aedtz`
- [ ] 確認: siwz の展開後エントリ構成と、バージョン取得元（`DB_VERSION` を流用できるか）。サンプル `aedt-launcher.Tests/TestData/BD-SAMPLE.DSGN_GD6.siwz`
- [ ] 決定: `ANSYSEM_ROOT<数値>` を使うか、使う場合の実行ファイルの組み立て方
- [ ] 決定: `App.config` と環境変数の優先順位
- [ ] 決定: #6 の未設定時表示方針（README の記載変更を含む）
- [ ] 確認: #19 のバージョンキーとインストール先の対応（実際の AEDT / Icepak で確認）

### 実装前に必要な作業

- README の「Supported Files」「Executable Configuration」の更新案を提示し、承認を得る。
