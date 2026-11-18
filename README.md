# aedt-launcher

English | [日本語](./README.ja.md)

A WPF launcher for ANSYS project files (Icepak / AEDT / SIwave). Drop a file or folder onto the window, or pass it as a command-line argument. The launcher detects the file type and version, then starts the matching ANSYS product.

## Purpose

When several ANSYS versions are installed, you must pick the right one for each file. This tool removes that step by:

- Detecting the file type and version automatically
- Launching the executable that matches the detected version
- Extracting Icepak Packfiles (`.tzr` / `.tgz`) from the window

## Supported Files

| Type | Detection | Where the version comes from |
| --- | --- | --- |
| AEDT | `.aedt` extension | The line after `begin 'Desktop'` in the file body |
| SIwave | `.siw` extension | The line containing `DB_VERSION` |
| ICEPAK Packfile | `.tzr` / `.tgz` extension | The `model` entry inside the archive |
| ICEPAK | Folder containing `model` | The `model` file directly under the folder |

## Usage

1. Drag and drop a file or folder onto the window. You can also pass it as a command-line argument.
2. The file name, type, and version are displayed.
3. Click **Launch** to start the executable for the detected version.
4. For an Icepak Packfile, click **Extract** to unpack the archive.

If the type cannot be detected, the type and version show `None`, and the buttons stay disabled.

## Executable Configuration

The executables are configured in the `appSettings` section of [App.config](./aedt-launcher/App.config). Keys use the format `Type:Version`.

```xml
<add key="ICEPAK:19.1" value="C:\Program Files\ANSYS Inc\v191\Icepak\bin\icepak19.1win64.bat" />
<add key="AEDT:2018.1" value="C:\Program Files\AnsysEM\AnsysEM19.1\Win64\ansysedt.exe" />
<add key="SIwave:2018.1" value="C:\Program Files\AnsysEM\AnsysEM19.1\Win64\siwave.exe" />
```

If no key matches the detected type and version, nothing is launched and no error is shown. Add keys and paths that match your installation.

## Build

- .NET 10 (`net10.0-windows`)
- Build with Visual Studio or `dotnet build`.

```powershell
dotnet build .\aedt-launcher\aedt-launcher.csproj -c Release
```

### Single-file publish

To distribute the launcher as a single executable with its dependencies embedded, use `dotnet publish`.

```powershell
dotnet publish .\aedt-launcher\aedt-launcher.csproj -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:Platform=x64 -o .\publish
```

The output folder contains `aedt-launcher.exe` and `aedt-launcher.dll.config`. Edit the config file next to the executable to change the `appSettings`.

The target machine needs the .NET 10 Desktop Runtime. Add `--self-contained true` to bundle the runtime instead (larger output).

## Dependencies

- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) 1.4.2 (used to extract Tar archives)

## License

This project is licensed under the MIT License. See [LICENSE](./LICENSE) for details.
