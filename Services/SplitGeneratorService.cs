using FbsDumper.Assembly;
using FbsDumper.Context;
using FbsDumper.Helpers;
using ZLinq;

namespace FbsDumper.Services;

public sealed class SplitGeneratorService(FileGenerationContext generation) : SchemaGeneratorServiceBase(generation)
{
    protected override void PrepareOutput()
    {
        if (!Directory.Exists(Generation.OutputPath))
            Directory.CreateDirectory(Generation.OutputPath);
    }

    protected override void GenerateCore(SchemaWriteContext context)
    {
        foreach (var fileContext in BuildFileContexts(context))
        {
            WriteSchemaFile(fileContext, context.EnumTypeNames);
            Log.Info($"Written: {Path.GetFileName(fileContext.OutputPath)}");
        }
    }

    protected override string GetSeparateEnumsPath()
    {
        return Path.Combine(Generation.OutputPath, "enums.fbs");
    }

    protected override void OnSeparateEnumsWritten(string filePath)
    {
        Log.Info($"Written: {Path.GetFileName(filePath)}");
    }

    protected override string ResolveFieldType(string fieldType, FlatField field, FlatTable table,
        FileWriteContext context, HashSet<string> enumTypeNames)
    {
        var fieldTypeNamespace = field.Type.Namespace ?? string.Empty;
        if (!enumTypeNames.Contains(fieldType) || fieldTypeNamespace == table.OriginalNamespace)
            return fieldType;

        var namespaceContext = NamespaceContext.Build(fieldTypeNamespace, Generation.CustomNamespace);
        return string.IsNullOrEmpty(namespaceContext.FinalNamespace)
            ? fieldType
            : $"{namespaceContext.FinalNamespace}.{fieldType}";
    }

    private IEnumerable<FileWriteContext> BuildFileContexts(SchemaWriteContext context)
    {
        var tablesByNamespace = context.Schema.FlatTables
            .AsValueEnumerable()
            .GroupBy(t => t.OriginalNamespace)
            .ToArray();

        foreach (var group in tablesByNamespace)
        {
            var tables = group.ToList();
            var namespaceContext = NamespaceContext.Build(group.Key, Generation.CustomNamespace);
            var fileName = BuildSchemaFileName(group.Key, Generation.CustomNamespace);
            var outputPath = Path.Combine(Generation.OutputPath, fileName);
            IReadOnlyList<FlatEnum> enums = Generation.EnumOut == EnumOut.Inline
                ? context.Schema.FlatEnums.AsValueEnumerable()
                    .Where(e => e.OriginalNamespace == group.Key)
                    .ToArray()
                : [];
            var includes = BuildIncludes(context, tables, namespaceContext.OriginalNamespace);
            yield return new FileWriteContext(outputPath, namespaceContext, tables, enums, includes, Generation);
        }
    }

    private List<string> BuildIncludes(SchemaWriteContext context, IReadOnlyList<FlatTable> tables,
        string currentNamespace)
    {
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Generation.EnumOut == EnumOut.Separate && ReferencesEnum(tables, context.EnumTypeNames))
            includes.Add("enums.fbs");

        foreach (var table in tables)
        foreach (var field in table.Fields)
        {
            var dependencyNamespace = field.Type.Namespace ?? string.Empty;
            if (dependencyNamespace == currentNamespace)
                continue;

            if (ShouldIncludeEnumFile(context, field, dependencyNamespace))
            {
                includes.Add(BuildSchemaFileName(dependencyNamespace, Generation.CustomNamespace));
                continue;
            }

            if (!context.TableNamespaces.Contains(dependencyNamespace))
                continue;

            includes.Add(BuildSchemaFileName(dependencyNamespace, Generation.CustomNamespace));
        }

        return [.. includes.AsValueEnumerable().OrderBy(x => x)];
    }

    private bool ShouldIncludeEnumFile(SchemaWriteContext context, FlatField field, string dependencyNamespace)
    {
        return Generation.EnumOut == EnumOut.Inline &&
               context.EnumTypeNames.Contains(field.Type.Name) &&
               context.EnumNamespaces.Contains(dependencyNamespace);
    }

    private static bool ReferencesEnum(IReadOnlyList<FlatTable> tables, HashSet<string> enumTypeNames)
    {
        return tables.Any(t => t.Fields.Any(f => enumTypeNames.Contains(f.Type.Name)));
    }
}
