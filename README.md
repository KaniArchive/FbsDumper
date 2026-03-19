# FbsDumper

A tool to recover FlatBuffer schema definitions from game assemblies with assembly instruction parsing.

*Originally made for **Blue Archive**, should theoretically work with other games but is untested.*

## Install

You can download the latest pre-build binaries at [Releases](https://github.com/KaniArchive/FbsDumper/releases)

[Windows](https://github.com/KaniArchive/FbsDumper/releases/latest/download/FbsDumper-win-x64.zip) | [Linux](https://github.com/KaniArchive/FbsDumper/releases/latest/download/FbsDumper-linux-x64.zip) | [MacOS](https://github.com/KaniArchive/FbsDumper/releases/latest/download/FbsDumper-osx-arm64.zip)

## Usage

```bash
# Show help
FbsDumper.exe --help

# Generate schema using assembly (single file)
FbsDumper.exe --dummy-dll "path/to/DummyDll" --game-assembly "path/to/libil2cpp.so"

# Generate schema without assembly
FbsDumper.exe --dummy-dll "path/to/DummyDll"

# Specify output file
FbsDumper.exe --dummy-dll "path/to/DummyDll" --output-file "MyGame.fbs"

# Split output into one .fbs per IL namespace, output-file is now a directory
FbsDumper.exe --dummy-dll "path/to/DummyDll" --split --output-file "./output"

# Split with a custom root namespace prefix
FbsDumper.exe --dummy-dll "path/to/DummyDll" --split --namespace "MyGame" --output-file "./output"

# Split with no root namespace (use original IL namespaces)
FbsDumper.exe --dummy-dll "path/to/DummyDll" --split --namespace "" --output-file "./output"

# Split enums into a separate enums.fbs (instead of inlining into each namespace file)
FbsDumper.exe --dummy-dll "path/to/DummyDll" --split --enum-out Separate --output-file "./output"

# Omit enums entirely
FbsDumper.exe --dummy-dll "path/to/DummyDll" --enum-out Omit

# Single file with enums in a separate file
FbsDumper.exe --dummy-dll "path/to/DummyDll" --enum-out Separate
```

## Build

1. Install [.NET SDK](https://dotnet.microsoft.com/en-us/download)
2. Clone this repository

```sh
git clone https://github.com/ArkanDash/FbsDumper
cd FbsDumper
```

3. Build using `dotnet`

```sh
dotnet build
```

## Options

- `-d, --dummy-dll`: Specifies the dummy DLL directory (Required)
- `-a, --game-assembly`: Specifies the path to libil2cpp.so (ARM) or GameAssembly.dll (x86/x64) (Optional: Skip assembly analysis)
- `-o, --output-file`: Specifies the output file or directory when using `--split` (Default: BlueArchive.fbs)
- `-n, --namespace`: Specifies the flatdata namespace. In `--split` mode acts as a root prefix prepended to each IL namespace; pass empty string to use IL namespaces verbatim (Default: FlatData)
- `-sp, --split`: Split output into one `.fbs` file per IL namespace, written flat into the output directory
- `-eo, --enum-out`: How to handle enums: `Inline` (default), `Separate` (single `enums.fbs` at root), `Omit` (skip enums)
- `-s, --force-snake-case`: Force snake case conversion
- `-nl, --namespace-to-look-for`: Specifies the namespace to look for
- `-f, --force`: Force processing using Add methods when no Create method exists
- `-sd, --skip-duplicates`: Skip types with duplicate short names, keeping only the first occurrence. By default all types are kept and a warning is emitted
- `-v, --verbose`: Enable verbose debug logging
- `-sw, --suppress-warnings`: Suppress warning messages

> [!IMPORTANT]
> **Disclaimer:** This software is made solely for educational purposes. I do not claim any responsibility for any usage
> of this software.

Copyright © 2025 [Hiro420](https://github.com/Hiro420)
