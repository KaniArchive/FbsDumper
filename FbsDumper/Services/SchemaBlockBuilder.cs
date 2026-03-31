using FbsDumper.Assembly;
using FbsDumper.Context;
using ZLinq;

namespace FbsDumper.Services;

internal sealed class SchemaBlockBuilder(FileGenerationContext gen)
{
    public List<SchemaBlock> Build(SchemaWriteContext schema)
    {
        var namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var ns in schema.Schema.FlatTables.AsValueEnumerable().Select(t => t.OriginalNamespace))
            namespaces.Add(ns);

        if (gen.EnumOut == EnumOut.Inline)
            foreach (var ns in schema.Schema.FlatEnums.AsValueEnumerable().Select(e => e.OriginalNamespace))
                namespaces.Add(ns);

        return Build(schema, [.. namespaces], gen.EnumOut == EnumOut.Inline);
    }

    public List<SchemaBlock> BuildEnums(SchemaWriteContext schema)
    {
        var namespaces = schema.Schema.FlatEnums
            .AsValueEnumerable()
            .Select(e => e.OriginalNamespace)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Build(schema, namespaces, false);
    }

    private List<SchemaBlock> Build(SchemaWriteContext schema, string[] namespaces, bool inlineEnums) =>
        namespaces
            .AsValueEnumerable()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(ns => new SchemaBlock(
                NamespaceContext.Build(ns, gen.CustomNamespace),
                schema.Schema.FlatTables
                    .AsValueEnumerable()
                    .Where(t => string.Equals(t.OriginalNamespace, ns, StringComparison.Ordinal))
                    .ToArray(),
                inlineEnums
                    ? schema.Schema.FlatEnums
                        .AsValueEnumerable()
                        .Where(e => string.Equals(e.OriginalNamespace, ns, StringComparison.Ordinal))
                        .ToArray()
                    : []))
            .Where(block => !block.IsEmpty)
            .ToList();
}