using FbsDumper.Assembly;
using FbsDumper.Context;
using FbsDumper.Helpers;
using ZLinq;

namespace FbsDumper.Services;

public class SplitGeneratorService(FileGenerationContext generation) : SchemaGeneratorServiceBase(generation)
{
    protected override void PrepareOutput()
    {
        if (!Directory.Exists(Generation.OutputPath))
            Directory.CreateDirectory(Generation.OutputPath);
    }

    protected override void GenerateCore(SchemaWriteContext context)
    {
        foreach (var block in Blocks.Build(context))
        {
            var outputPath = Path.Combine(
                Generation.OutputPath,
                BuildSchemaFileName(block.Namespace.OriginalNamespace, Generation.CustomNamespace));
            var includes = BuildIncludes(context, block.Tables, block.Namespace.OriginalNamespace);
            var file = CreateFile(outputPath, block, includes, true);

            WriteSchemaFile(file, context);
            Log.Info($"Written: {Path.GetFileName(outputPath)}");
        }
    }

    protected override string GetSeparateEnumsPath() => Path.Combine(Generation.OutputPath, "enums.fbs");

    private List<string> BuildIncludes(SchemaWriteContext context, IReadOnlyList<FlatTable> tables,
        string currentNs)
    {
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Generation.EnumOut == EnumOut.Separate && ReferencesEnum(tables, context.Lookup))
            includes.Add("enums.fbs");

        foreach (var table in tables)
        foreach (var field in table.Fields)
        {
            var depNs = field.Type.Namespace ?? string.Empty;
            if (depNs == currentNs)
                continue;

            if (ShouldIncludeEnumFile(context.Lookup, field, depNs))
            {
                includes.Add(BuildSchemaFileName(depNs, Generation.CustomNamespace));
                continue;
            }

            if (!context.Lookup.TableNamespaces.Contains(depNs))
                continue;

            includes.Add(BuildSchemaFileName(depNs, Generation.CustomNamespace));
        }

        return [.. includes.AsValueEnumerable().OrderBy(x => x)];
    }

    private bool ShouldIncludeEnumFile(SchemaLookupContext lookup, FlatField field, string depNs) =>
        Generation.EnumOut == EnumOut.Inline &&
        lookup.EnumNames.Contains(field.Type.Name) &&
        lookup.EnumNamespaces.Contains(depNs);
}