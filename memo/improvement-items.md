# aedt-launcher 改善項目

対象: `aedt-launcher/` (WPF / .NET 10, `net10.0-windows`)

## 1. 不正なファイルで例外が未処理のまま落ちる可能性

- 場所: `FileClass.cs` の `checkSAversion`、`checkAEDTversion`、`checkSIversion`、`extrctSAversion`
- 内容: gzip でない `.tgz`、壊れた `.aedt` / `.siw` などで例外が発生しても捕捉していない。
- 影響: ドロップ時にアプリケーションが異常終了する可能性がある。
- 終了フラグ: [ ]

## 2. 複数ファイル指定時に最後の1件しか表示されない

- 場所: `MainWindow.xaml.cs` の `checkFile(string[])`
- 内容: ループ内で毎回テキストボックスを上書きしているため、最後のファイルの結果だけが残る。
- 影響: 複数ファイルをドロップ/引数指定したときに、先頭のファイルの情報が見えない。
- 終了フラグ: [ ]

## 3. Ctrl+Q でアプリが終了しない

- 場所: `MainWindow.xaml` の `File > Quit`
- 内容: メニューの `InputGestureText="Ctrl+Q"` は表示のみで、実際のショートカット（`KeyBinding`）が定義されていないため、Ctrl+Q を押してもアプリが終了しない。
- 対応案: `Window` に `KeyBinding`（Ctrl+Q → `MenuItem_Click` 相当の終了処理）を追加する。
- 補足: 既存の終了処理（`app_Exit`）を再利用できるため、変更は小さい。
- 終了フラグ: [ ]

## 4. 起動時に実行ファイル自身のパスを解析対象にしている

- 場所: `MainWindow.xaml.cs` の `Window_Loaded`
- 内容: `checkFile(args)` に `args[0]`（実行ファイル自身のパス）も渡している。
- 影響: 解析対象外のパスを処理しており、無駄な処理になっている。
- 終了フラグ: [ ]

## 5. Launch の失敗・未設定が無表示

- 場所: `FileClass.cs` の `launchApplication`
- 内容: `catch {}` が空で、`App.config` に設定キーがない場合や起動失敗時に何も通知されない。
- 補足: README には「キーがない場合は何も起動せずエラーも表示しない」と記載されている。表示方針を変更する場合は README も併せて更新が必要。
- 終了フラグ: [ ]

## 6. aedtz ファイル（7-Zip 形式）への対応

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

## 7. siwz ファイル（7-Zip 形式）への対応

- 対象: 拡張子 `.siwz` の追加
- 内容: `.siwz` も 7-Zip 形式のため、項目 6 と同じメモリ上の読み取り処理を共通化する。
- サンプル: `data/BD-SAMPLE.DSGN_GD6.siwz`
- 要確認: 展開後のエントリ構成と、バージョンの取得元（現在の `.siw` の `DB_VERSION` 判定を流用できるか）。
- 補足: README の「Supported Files」表の更新が必要。
- 終了フラグ: [ ]

## 8. 環境変数から AEDT の実行パスを取得

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
- 補足: 項目 5（未設定時の表示）と関連する。README の「Executable Configuration」の更新が必要。
- 終了フラグ: [ ]

## 9. アプリアイコンがさみしい

- 内容: アプリアイコンの見た目が寂しい印象になっている。
- 原因: `aedt-launcher.csproj` で `ApplicationIcon` を指定していないため、デフォルトアイコンが使われている。
- 作り方:
  1. 256×256px 以上の透過PNGを作成する（Inkscape / GIMP / Figma などで作成）。
  2. `magick input.png -define icon:auto-resize=256,48,32,24,16 app.ico` で複数サイズの ICO に変換し、`Images\` に置く。
  3. csproj に `<ApplicationIcon>Images\app.ico</ApplicationIcon>` を追加する。
- 補足: 具体的なデザイン方針は未定。
- 終了フラグ: [ ]

## 10. 解析ロジックの単体テストがない

- 対象: AEDT / SIwave / Icepak のバージョン判定（`checkAEDTversion`、`checkSIversion`、`checkSAdirectory`、`ParseIcepakVersion`）
- 内容: テストプロジェクトが存在しないため、解析処理の回帰を検出する手段がない。
- 補足: テストプロジェクトの追加は依存関係の変更を伴うため、実施前に承認が必要。
- 終了フラグ: [x]

## 11. パス区切りのハードコード

- 場所: `FileClass.cs` の `checkSAversion`
- 内容: `Path.GetDirectoryName(fi) + "\\" + te.Name.Split('/')[0]` のように `"\\"` を直接連結している。
- 影響: `Path.Combine` を使うと安全に組み立てられる。
- 終了フラグ: [ ]

## 12. 未使用コード・命名・リソース管理の整理

- 未使用の変数・型: `MainWindow.xaml.cs` の `list`（`MyFileList`）、`App.xaml.cs` の未使用 `using`
- typo / 命名: `extrctSAversion`（extract）、`get_filetye`（filetype）
- 命名規則の不統一: `get_filename` / `get_fileversion` などの `get_` 接頭辞（プロパティにする）、`checkFILE` の大文字（`CheckFile`）、`checkSIversion` などの PascalCase 化、`filename` などの private フィールドに `_` 接頭辞がない、`fi` / `ar` / `te` / `li` などの短い変数名
- リソース: `launchApplication` の `Process` が Dispose されていない
- 補足: 公開メソッド名を変更するとすべての呼び出し元の修正が必要になる。
- 終了フラグ: [ ]

## 13. 展開処理が Launch の中で実行される・状態を flag で持っている

- 場所: `FileClass.cs` の `launchApplication`、`extrctSAversion`、フィールド `flag`
- 内容: 「Launch」の処理の中で `flag == 0` の場合に展開（副作用）が走る。展開済みかどうかを `int flag` で判断しており、意味が分かりにくい。
- 影響: 展開の失敗時の扱いが Launch 側と分離されていない。状態の受け渡しがフィールドに依存している。
- 対応案: 判定結果（種類・バージョン・パス）を 1 つのオブジェクトにまとめ、展開は別メソッドとして明示的に呼ぶ。`flag` は `bool` にする。
- 終了フラグ: [ ]

## 14. UI の表示文字列で処理を分岐している・種類を文字列で管理している

- 場所: `MainWindow.xaml.cs` の `checkFile`、`FileClass.cs` の `filetype` / `launchApplication`
- 内容: `textBox_Type.Text` と `"None"` / `"ICEPAK Packfile"` を比較して Extract / Launch ボタンの有効・無効を決めている。種類も `"ICEPAK"`、`"AEDT"`、`"SIwave"` などの文字列で扱っている。
- 影響: 表示文字列を変えると動作が壊れる。タイプミスをコンパイラが検出できない。
- 対応案: 種類を `enum` にし、ボタンの状態は FileClass 側の判定結果から決める。
- 終了フラグ: [ ]

## 15. 拡張子判定が大文字小文字を区別する・Regex の使い方が広い

- 場所: `FileClass.cs` の `checkFILE`（`switch (ext)`）、`checkSAversion`（`Regex.IsMatch(te.Name, "model$")`）、`checkSIversion` / `checkAEDTversion`
- 内容:
  - `.TZR` や `.AEDT` など大文字の拡張子が認識されない。
  - `"model$"` は `xmodel` のような名前にも一致する。
  - `"DB_VERSION"` や `"begin 'Desktop'"` の判定は `Contains` で十分である。
- 対応案: 拡張子は大文字小文字を無視して比較する。tar のエントリ名は `Path.GetFileName(te.Name) == "model"` のように完全一致で判定する。
- 終了フラグ: [ ]

## 16. 展開先の上書き確認がなく、展開後のフィードバックもない

- 場所: `FileClass.cs` の `extrctSAversion`、`MainWindow.xaml.cs` の `button_Extract_Click`
- 内容:
  - 展開先は元ファイルと同じフォルダで、既存のファイル・フォルダがあっても確認せず上書きする。
  - 展開が成功したか、どこに展開されたかが画面に表示されない。
  - Extract ボタンは押すたびに展開を実行する。
- 対応案: 展開先が既に存在する場合は確認する。展開完了後にパスを表示し、展開済みならボタンを無効にする。
- 終了フラグ: [ ]

## 17. App.config のバージョンキーとインストール先の対応を確認していない

- 場所: [App.config](d:/+Projects/repos/aedt-launcher/aedt-launcher/App.config)
- 内容:
  - `AEDT:2019.1` が `AnsysEM19.3` フォルダを指している。`AEDT:2018.x` のキーは `AnsysEM19.x` フォルダを指しており、キーの年とフォルダ名の対応が揃っていない。
  - インストール先パスがハードコードされているため、環境によっては起動できない。
- 対応案: ファイル内のバージョン表記（`begin 'Desktop'` の値など）と各キーの対応を、実際の AEDT / Icepak のバージョンで確認する。
- 補足: 項目 8 の環境変数対応を行う場合は、この確認結果を前提にする。
- 終了フラグ: [ ]

## 18. app.manifest・AssemblyInfo.cs がテンプレートのまま

- 場所: [app.manifest](d:/+Projects/repos/aedt-launcher/aedt-launcher/Properties/app.manifest)、[AssemblyInfo.cs](d:/+Projects/repos/aedt-launcher/aedt-launcher/Properties/AssemblyInfo.cs)
- 内容:
  - `assemblyIdentity name="MyApplication.app"` が初期値のまま。
  - `applicationRequestMinimum` の `PermissionSet` は .NET Framework の旧機能で、.NET 10 では効果がない。
  - `AssemblyCopyright` の年が 2019 のまま。
- 対応案: manifest は `requestedExecutionLevel` 以外を削除し、名前を実際のアプリ名に合わせる。著作権年は意図を確認して更新する。
- 終了フラグ: [x]

## 19. 「About」メニューに処理が割り当てられていない

- 場所: `MainWindow.xaml` の `Help > About`
- 内容: `MenuItem` に `Click` ハンドラがなく、クリックしても何も起きない。
- 対応案: About ダイアログを表示するか、項目を削除する。どちらにするかは仕様として決める必要がある。
- 終了フラグ: [ ]

## 20. csproj と未使用ファイルの整理

- 場所: [aedt-launcher.csproj](d:/+Projects/repos/aedt-launcher/aedt-launcher/aedt-launcher.csproj)、[Settings.settings](d:/+Projects/repos/aedt-launcher/aedt-launcher/Properties/Settings.settings)
- 内容:
  - `ErrorReport` と `WarningLevel` は SDK スタイルの csproj では不要。
  - `<None Include="App.config" />` は `<AppConfig>` と重複している。
  - `Settings.settings` は空で、使われていない。
  - `App.xaml.cs` の未使用 `using` は項目 12 で扱う。
- 対応案: 不要な項目を削除する。`Settings.settings` は `Settings.Designer.cs` と合わせて削除する。
- 終了フラグ: [x]

## 方針・未決事項のまとめ（6・7・8）

### 方針

- **aedtz / siwz（項目 6・7）**: テンポラリへ展開せず、メモリ上で 7z を読み、判定に必要なエントリだけを文字列として取り出す。
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
- [ ] 決定: 項目 5 の未設定時表示方針（README の記載変更を含む）
- [ ] 確認: 項目 17 のバージョンキーとインストール先の対応（実際の AEDT / Icepak で確認）
- [ ] 決定: 項目 19 の About メニューを実装するか、削除するか

### 実装前に必要な作業

- README の「Supported Files」「Executable Configuration」の更新案を提示し、承認を得る。
- 項目 10（単体テスト）を実施する場合は、テストプロジェクト追加の承認を得る。
