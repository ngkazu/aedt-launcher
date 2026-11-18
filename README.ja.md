# aedt-launcher

[English](./README.md) | 日本語

ANSYS の解析ファイル（Icepak / AEDT / SIwave）をドラッグ＆ドロップまたはコマンドライン引数で受け取り、ファイルに対応するバージョンの ANSYS 製品を判定して起動する WPF ランチャーです。

## 目的・意図

複数バージョンの ANSYS がインストールされている環境では、ファイルごとに正しいバージョンを選んで開く必要があります。本ツールは次の手間を省くことを目的としています。

- ファイルの種類とバージョンを自動判定する
- 判定したバージョンに対応する実行ファイルで起動する
- Icepak Packfile（`.tzr` / `.tgz`）を画面から展開する

## 対応ファイル

| 種類 | 判定条件 | バージョンの取得元 |
| --- | --- | --- |
| AEDT | 拡張子 `.aedt` | 本文中の `begin 'Desktop'` の次行 |
| SIwave | 拡張子 `.siw` | `DB_VERSION` を含む行 |
| ICEPAK Packfile | 拡張子 `.tzr` / `.tgz` | アーカイブ内の `model` エントリ |
| ICEPAK | `model` を含むフォルダ | フォルダ直下の `model` ファイル |

## 使い方

1. アプリケーションのウィンドウに、ファイルまたはフォルダをドラッグ＆ドロップします。コマンドライン引数で渡すこともできます。
2. ファイル名、種類、バージョンが表示されます。
3. **Launch** で対応するバージョンの実行ファイルを起動します。
4. Icepak Packfile の場合は **Extract** でアーカイブを展開できます。

種類が判定できない場合は、種類とバージョンに `None` と表示され、ボタンは無効のままです。

## 実行ファイルの設定

起動する実行ファイルは [App.config](./aedt-launcher/App.config) の `appSettings` で指定します。キーは `種類:バージョン` の形式です。

```xml
<add key="ICEPAK:19.1" value="C:\Program Files\ANSYS Inc\v191\Icepak\bin\icepak19.1win64.bat" />
<add key="AEDT:2018.1" value="C:\Program Files\AnsysEM\AnsysEM19.1\Win64\ansysedt.exe" />
<add key="SIwave:2018.1" value="C:\Program Files\AnsysEM\AnsysEM19.1\Win64\siwave.exe" />
```

判定したバージョンに対応するキーがない場合、起動は行われず、エラーも表示されません。インストール環境に合わせてパスとキーを追加してください。

## ビルド

- .NET 10（`net10.0-windows`）
- Visual Studio または `dotnet build` でビルドできます。

```powershell
dotnet build .\aedt-launcher\aedt-launcher.csproj -c Release
```

### 単一ファイルとしての発行

依存 DLL をまとめた単一の実行ファイルとして配布するには、`dotnet publish` を使います。

```powershell
dotnet publish .\aedt-launcher\aedt-launcher.csproj -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Platform=x64 -o .\publish
```

出力フォルダには `aedt-launcher.exe` と `aedt-launcher.dll.config` が作られます。設定を変更する場合は、exe と同じフォルダにある設定ファイルの `appSettings` を編集してください。

実行先には .NET 10 Desktop Runtime が必要です。ランタイムを同梱する場合は `--self-contained true` を指定します（出力サイズは大きくなります）。

## 依存パッケージ

- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) 1.4.2（Tar の展開に使用）

## ライセンス

本プロジェクトは MIT ライセンスで提供されます。詳細は [LICENSE](./LICENSE) を参照してください。
