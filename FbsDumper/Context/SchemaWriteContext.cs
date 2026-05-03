using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct SchemaWriteContext
{
    private SchemaWriteContext(FlatSchema schema, SchemaLookupContext lookup)
    {
        Schema = schema;
        Lookup = lookup;
    }

    public FlatSchema Schema { get; }
    public SchemaLookupContext Lookup { get; }

    public static SchemaWriteContext Build(FlatSchema schema) =>
        new(schema, SchemaLookupContext.Build(schema));
}