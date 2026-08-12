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

# Split enums into a separate enums.fbs (instead of inlining into each namespace file)
FbsDumper.exe --dummy-dll "path/to/DummyDll" --split --enum-out Separate --output-file "./output"
```

## Build

1. Install [.NET SDK](https://dotnet.microsoft.com/en-us/download)
2. Clone this repository

```sh
git clone https://github.com/KaniArchive/FbsDumper
cd FbsDumper/FbsDumper.CLI
```

3. Build using `dotnet`

```sh
dotnet build
```

## Options

- `-d, --dummy-dll`: Specifies the dummy DLL directory (Required)
- `-a, --game-assembly`: Specifies the path to libil2cpp.so (ARM) or GameAssembly.dll (x86/x64) (Optional: Skip assembly analysis)
- `-o, --output-file`: Specifies the output file or directory when using `--split` (Default: BlueArchive.fbs)
- `-n, --namespace`: Specifies the flatdata namespace. In `--split` mode acts as a root prefix prepended to each IL namespace
- `-sp, --split`: Split output into one `.fbs` file per IL namespace, written flat into the output directory
- `-eo, --enum-out`: How to handle enums: `Inline` (default), `Separate` (single `enums.fbs` at root), `Omit` (skip enums)
- `-s, --force-snake-case`: Force snake case conversion
- `-nl, --namespace-to-look-for`: Specifies the namespace to look for
- `-f, --force`: Force processing using Add methods when no Create method exists
- `-st, --shorten-types`: Force short type names instead of namespace-qualified type names
- `-sd, --skip-duplicates`:  Keep only the first occurrence of duplicate short names
- `-v, --verbose`: Enable verbose debug logging
- `-sw, --suppress-warnings`: Suppress warning messages


> [!IMPORTANT]
> **Disclaimer:** This software is made solely for educational purposes. I do not claim any responsibility for any usage
> of this software.

## License

`FbsDumper` is under **GPL v3**. See [LICENSE](LICENSE) for copyright and license details.
