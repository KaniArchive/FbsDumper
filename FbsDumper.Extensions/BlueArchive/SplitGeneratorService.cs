using FbsDumper.Assembly;
using FbsDumper.Context;
using Utf8StringInterpolation;

namespace FbsDumper.Extensions.BlueArchive;

public class SingleGeneratorService(FileGenerationContext generation) : Services.SingleGeneratorService(generation)
{
    protected override string ResolveFieldType(string typeName, FieldWriteContext field) => typeName;

    protected override void WriteTable<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatTable table,
        FileWriteContext file, SchemaWriteContext schema)
    {
        var isEncrypted = table.Metadata.TryGetValue("HasEncryption", out var v) && v is true;
        var tableDecl = isEncrypted ? $"table {table.TableName} (encrypted)" : $"table {table.TableName}";
        writer.AppendFormat($"{tableDecl} {{\n");

        if (table.NoCreate) writer.AppendLiteral("\t// No Create method\n");

        foreach (var field in table.Fields)
            WriteField(ref writer, new FieldWriteContext(field, table, file, schema));

        writer.AppendLiteral("}\n");
    }
}

public class SplitGeneratorService(FileGenerationContext generation) : Services.SplitGeneratorService(generation)
{
    protected override string ResolveFieldType(string typeName, FieldWriteContext field) => typeName;

    protected override void WriteTable<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatTable table,
        FileWriteContext file, SchemaWriteContext schema)
    {
        var isEncrypted = table.Metadata.TryGetValue("HasEncryption", out var v) && v is true;
        var tableDecl = isEncrypted ? $"table {table.TableName} (encrypted)" : $"table {table.TableName}";
        writer.AppendFormat($"{tableDecl} {{\n");

        if (table.NoCreate) writer.AppendLiteral("\t// No Create method\n");

        foreach (var field in table.Fields)
            WriteField(ref writer, new FieldWriteContext(field, table, file, schema));

        writer.AppendLiteral("}\n");
    }
}
