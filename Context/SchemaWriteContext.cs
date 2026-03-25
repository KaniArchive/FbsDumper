using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct SchemaWriteContext(
    FlatSchema Schema,
    FileGenerationContext Generation,
    HashSet<string> EnumTypeNames,
    HashSet<string> TableNamespaces,
    HashSet<string> EnumNamespaces
)
{
    public static SchemaWriteContext Build(FlatSchema schema, FileGenerationContext generation)
    {
        var enumTypeNames = new HashSet<string>(
            schema.FlatEnums.Select(e => e.EnumName),
            StringComparer.OrdinalIgnoreCase);
        var tableNamespaces = new HashSet<string>(schema.FlatTables.Select(t => t.OriginalNamespace));
        var enumNamespaces = new HashSet<string>(schema.FlatEnums.Select(e => e.OriginalNamespace));
        return new SchemaWriteContext(schema, generation, enumTypeNames, tableNamespaces, enumNamespaces);
    }
}