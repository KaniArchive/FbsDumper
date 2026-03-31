using FbsDumper.Assembly;
using FbsDumper.Context;
using ZLinq;

namespace FbsDumper.Services;

public class SingleGeneratorService(FileGenerationContext generation) : SchemaGeneratorServiceBase(generation)
{
    private string OutputPath =>
        Directory.Exists(Generation.OutputPath)
            ? Path.Combine(
                Generation.OutputPath,
                BuildSchemaFileName(string.Empty, Generation.CustomNamespace))
            : Generation.OutputPath;

    protected override void GenerateCore(SchemaWriteContext schema)
    {
        if (!schema.Lookup.HasDuplicates)
        {
            WriteSchemaFile(BuildFile(schema), schema);
            return;
        }

        var files = Blocks.Build(schema)
            .AsValueEnumerable()
            .Select(block => CreateFile(OutputPath, block, [], true))
            .ToArray();
        var includes = BuildIncludes(schema.Schema.FlatTables, schema.Lookup);
        WriteSchemaFile(OutputPath, files, includes, schema);
    }

    protected override string GetSeparateEnumsPath() =>
        Path.Combine(
            Path.GetDirectoryName(OutputPath) ?? ".",
            Path.GetFileNameWithoutExtension(OutputPath) + ".enums.fbs");

    private FileWriteContext BuildFile(SchemaWriteContext schema)
    {
        var ns = NamespaceContext.Build(string.Empty, Generation.CustomNamespace);
        IReadOnlyList<FlatEnum> enums = Generation.EnumOut == EnumOut.Inline
            ? schema.Schema.FlatEnums
            : [];
        var includes = BuildIncludes(schema.Schema.FlatTables, schema.Lookup);

        return CreateFile(
            OutputPath,
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