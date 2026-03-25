using FbsDumper.Assembly;
using FbsDumper.Context;

namespace FbsDumper.Services;

public sealed class SingleGeneratorService(FileGenerationContext generation) : SchemaGeneratorServiceBase(generation)
{
    protected override void GenerateCore(SchemaWriteContext context)
    {
        var fileContext = BuildFileContext(context);
        WriteSchemaFile(fileContext, context.EnumTypeNames);
    }

    protected override string GetSeparateEnumsPath()
    {
        return Path.Combine(
            Path.GetDirectoryName(Generation.OutputPath) ?? ".",
            Path.GetFileNameWithoutExtension(Generation.OutputPath) + ".enums.fbs");
    }

    private FileWriteContext BuildFileContext(SchemaWriteContext context)
    {
        var fileNamespace = NamespaceContext.Build(string.Empty, Generation.CustomNamespace);
        IReadOnlyList<FlatEnum> enums = Generation.EnumOut == EnumOut.Inline
            ? context.Schema.FlatEnums
            : [];

        return new FileWriteContext(
            Generation.OutputPath,
            fileNamespace,
            context.Schema.FlatTables,
            enums,
            [],
            Generation);
    }
}