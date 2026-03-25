using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct FileWriteContext(
    string OutputPath,
    NamespaceContext Namespace,
    IReadOnlyList<FlatTable> Tables,
    IReadOnlyList<FlatEnum> Enums,
    IReadOnlyList<string> Includes,
    FileGenerationContext Generation
);