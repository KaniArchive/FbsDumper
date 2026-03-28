using FbsDumper.Assembly;
using FbsDumper.Context;
using ZLinq;

namespace FbsDumper.Services;

public sealed class SingleGeneratorService(FileGenerationContext generation) : SchemaGeneratorServiceBase(generation)
{
    protected override void GenerateCore(SchemaWriteContext schema)
    {
        if (!schema.Lookup.HasDuplicates)
        {
            WriteSchemaFile(BuildFile(schema), schema);
            return;
        }

        var files = Blocks.Build(schema)
            .AsValueEnumerable()
            .Select(block => CreateFile(Generation.OutputPath, block, [], true))
            .ToArray();
        var includes = BuildIncludes(schema.Schema.FlatTables, schema.Lookup);
        WriteSchemaFile(Generation.OutputPath, files, includes, schema);
    }

    protected override string GetSeparateEnumsPath() =>
        Path.Combine(
            Path.GetDirectoryName(Generation.OutputPath) ?? ".",
            Path.GetFileNameWithoutExtension(Generation.OutputPath) + ".enums.fbs");

    private FileWriteContext BuildFile(SchemaWriteContext schema)
    {
        var ns = NamespaceContext.Build(string.Empty, Generation.CustomNamespace);
        IReadOnlyList<FlatEnum> enums = Generation.EnumOut == EnumOut.Inline
            ? schema.Schema.FlatEnums
            : [];
        var includes = BuildIncludes(schema.Schema.FlatTables, schema.Lookup);

        return CreateFile(
            Generation.OutputPath,
            new SchemaBlock(
                ns,
                schema.Schema.FlatTables,
                enums),
            includes,
            false);
    }

    private List<string> BuildIncludes(IReadOnlyList<FlatTable> tables, SchemaLookupContext lookup)
    {
        if (Generation.EnumOut != EnumOut.Separate || !ReferencesEnum(tables, lookup))
            return [];

        return [Path.GetFileName(GetSeparateEnumsPath())];
    }
}
