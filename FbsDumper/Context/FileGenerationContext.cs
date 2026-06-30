using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct FileGenerationContext(
    string OutputPath,
    string? CustomNamespace,
    EnumOut EnumOut,
    bool ForceSnakeCase,
    bool IsSplitMode,
    bool SkipDuplicates
);
