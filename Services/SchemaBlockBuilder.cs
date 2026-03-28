using FbsDumper.Assembly;
using FbsDumper.Context;

namespace FbsDumper.Services;

internal sealed class SchemaBlockBuilder(FileGenerationContext gen)
{
    public List<SchemaBlock> Build(SchemaWriteContext schema)
    {
        var namespaces = new HashSet<string>(
            schema.Schema.FlatTables.Select(t => t.OriginalNamespace),
            StringComparer.OrdinalIgnoreCase);

        if (gen.EnumOut == EnumOut.Inline)
            foreach (var ns in schema.Schema.FlatEnums.Select(e => e.OriginalNamespace))
                namespaces.Add(ns);

        return Build(schema, namespaces, gen.EnumOut == EnumOut.Inline);
    }

    public List<SchemaBlock> BuildEnums(SchemaWriteContext schema)
    {
        var namespaces = schema.Schema.FlatEnums
            .Select(e => e.OriginalNamespace)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Build(schema, namespaces, false);
    }

    private List<SchemaBlock> Build(SchemaWriteContext schema, IEnumerable<string> namespaces, bool inlineEnums) =>
        namespaces
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(ns => new SchemaBlock(
                NamespaceContext.Build(ns, gen.CustomNamespace),
                schema.Schema.FlatTables
                    .Where(t => string.Equals(t.OriginalNamespace, ns, StringComparison.Ordinal))
                    .ToArray(),
                inlineEnums
                    ? schema.Schema.FlatEnums
                        .Where(e => string.Equals(e.OriginalNamespace, ns, StringComparison.Ordinal))
                        .ToArray()
                    : []))
            .Where(block => !block.IsEmpty)
            .ToList();
}