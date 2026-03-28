using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct SchemaLookupContext(
    HashSet<string> TableNames,
    HashSet<string> EnumNames,
    HashSet<string> DuplicateTableNames,
    HashSet<string> DuplicateEnumNames,
    HashSet<string> TableNamespaces,
    HashSet<string> EnumNamespaces)
{
    public bool HasDuplicates => DuplicateTableNames.Count > 0 || DuplicateEnumNames.Count > 0;

    public bool HasType(string name) => TableNames.Contains(name) || EnumNames.Contains(name);

    public static SchemaLookupContext Build(FlatSchema schema) =>
        new(
            BuildSet(schema.FlatTables.Select(t => t.TableName)),
            BuildSet(schema.FlatEnums.Select(e => e.EnumName)),
            BuildDuplicateSet(schema.FlatTables.Select(t => t.TableName)),
            BuildDuplicateSet(schema.FlatEnums.Select(e => e.EnumName)),
            BuildSet(schema.FlatTables.Select(t => t.OriginalNamespace)),
            BuildSet(schema.FlatEnums.Select(e => e.OriginalNamespace)));

    private static HashSet<string> BuildSet(IEnumerable<string> values) =>
        new(values, StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> BuildDuplicateSet(IEnumerable<string> values) =>
        new(
            values
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key),
            StringComparer.OrdinalIgnoreCase);
}
