using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct SchemaWriteContext
{
    public FlatSchema Schema { get; }
    public SchemaLookupContext Lookup { get; }

    private SchemaWriteContext(FlatSchema schema, SchemaLookupContext lookup)
    {
        Schema = schema;
        Lookup = lookup;
    }

    public static SchemaWriteContext Build(FlatSchema schema) =>
        new(schema, SchemaLookupContext.Build(schema));
}
