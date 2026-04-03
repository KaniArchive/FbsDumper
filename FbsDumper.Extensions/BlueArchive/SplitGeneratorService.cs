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
        writer.AppendFormat($"{SchemaFmt.GetTableDecl(table)} {{\n");

        if (table.NoCreate && table.Fields.Count == 0)
            writer.AppendLiteral("\t// No Create method\n");

        foreach (var field in table.Fields)
            WriteField(ref writer, new FieldWriteContext(field, table, file, schema));

        writer.AppendLiteral("}\n");
    }

    protected override void WriteField<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FieldWriteContext field) =>
        SchemaFmt.WriteField(ref writer, field, GetFieldName(field), GetFieldType(field));
}

public class SplitGeneratorService(FileGenerationContext generation) : Services.SplitGeneratorService(generation)
{
    protected override string ResolveFieldType(string typeName, FieldWriteContext field) => typeName;

    protected override void WriteTable<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatTable table,
        FileWriteContext file, SchemaWriteContext schema)
    {
        writer.AppendFormat($"{SchemaFmt.GetTableDecl(table)} {{\n");

        if (table.NoCreate && table.Fields.Count == 0)
            writer.AppendLiteral("\t// No Create method\n");

        foreach (var field in table.Fields)
            WriteField(ref writer, new FieldWriteContext(field, table, file, schema));

        writer.AppendLiteral("}\n");
    }

    protected override void WriteField<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FieldWriteContext field) =>
        SchemaFmt.WriteField(ref writer, field, GetFieldName(field), GetFieldType(field));
}

internal static class SchemaFmt
{
    public static string GetTableDecl(FlatTable table)
    {
        List<string> attributes = [];

        if (IsEncryptedTable(table))
            attributes.Add("encrypted");

        if (table.NoCreate)
            attributes.Add("no_create");

        return attributes.Count == 0
            ? $"table {table.TableName}"
            : $"table {table.TableName} ({string.Join(", ", attributes)})";
    }

    public static void WriteField<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FieldWriteContext field,
        string fieldName, string fieldType)
        where TBufferWriter : System.Buffers.IBufferWriter<byte>
    {
        var encrypted = IsEncryptedField(field) ? " (encrypted)" : string.Empty;
        writer.AppendFormat($"\t{fieldName}: {fieldType}{encrypted}; // index 0x{field.Field.Offset:X}\n");
    }

    private static bool IsEncryptedField(FieldWriteContext field) =>
        IsEncryptedTable(field.Table) &&
        field.Field.Type.FullName != "System.Boolean";

    private static bool IsEncryptedTable(FlatTable table) =>
        table.Metadata.TryGetValue("HasEncryption", out var value) &&
        value is true;
}
