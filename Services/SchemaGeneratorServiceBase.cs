using System.Buffers;
using System.Text.RegularExpressions;
using FbsDumper.Assembly;
using FbsDumper.Context;
using Utf8StringInterpolation;

namespace FbsDumper.Services;

public abstract partial class SchemaGeneratorServiceBase
{
    protected SchemaGeneratorServiceBase(FileGenerationContext generation)
    {
        Generation = generation;
    }

    protected FileGenerationContext Generation { get; }

    public void Generate(FlatSchema schema)
    {
        var context = SchemaWriteContext.Build(schema, Generation);
        PrepareOutput();
        GenerateCore(context);
        WriteSeparateEnumsIfNeeded(schema);
    }

    protected virtual void PrepareOutput()
    {
    }

    protected abstract void GenerateCore(SchemaWriteContext context);

    protected abstract string GetSeparateEnumsPath();

    protected virtual void OnSeparateEnumsWritten(string filePath)
    {
    }

    protected void WriteSchemaFile(FileWriteContext context, HashSet<string> enumTypeNames)
    {
        using var buffer = Utf8String.CreateWriter(out var stringWriter);
        WriteSchemaContent(ref stringWriter, context, enumTypeNames);
        stringWriter.Flush();
        File.WriteAllBytes(context.OutputPath, buffer.ToArray());
    }

    protected void WriteEnumsFile(List<FlatEnum> enums, string filePath)
    {
        using var buffer = Utf8String.CreateWriter(out var stringWriter);
        if (!string.IsNullOrEmpty(Generation.CustomNamespace))
            stringWriter.AppendFormat($"namespace {Generation.CustomNamespace};\n\n");

        foreach (var flatEnum in enums)
        {
            WriteTableEnum(ref stringWriter, flatEnum);
            stringWriter.AppendLine();
        }

        stringWriter.Flush();
        File.WriteAllBytes(filePath, buffer.ToArray());
    }

    protected virtual bool ShouldEscapeFieldName(string fieldName, string tableName, string fieldType,
        HashSet<string> enumTypeNames)
    {
        return false;
    }

    protected virtual string ResolveFieldType(string fieldType, FlatField field, FlatTable table,
        FileWriteContext context, HashSet<string> enumTypeNames)
    {
        return fieldType;
    }

    protected static string BuildSchemaFileName(string originalNamespace, string? customNamespace)
    {
        var namespaceContext = NamespaceContext.Build(originalNamespace, customNamespace);
        return string.IsNullOrEmpty(namespaceContext.FinalNamespace)
            ? "tables.fbs"
            : $"{namespaceContext.FinalNamespace}.fbs";
    }

    protected static string CamelToSnake(string camelStr)
    {
        var isAllUppercase = camelStr.All(char.IsUpper);
        if (string.IsNullOrEmpty(camelStr) || isAllUppercase)
            return camelStr;
        return CamelToSnakeRegex().Replace(camelStr, "$1_").ToLower();
    }

    private void WriteSeparateEnumsIfNeeded(FlatSchema schema)
    {
        if (Generation.EnumOut != EnumOut.Separate || schema.FlatEnums.Count <= 0)
            return;

        var filePath = GetSeparateEnumsPath();
        WriteEnumsFile(schema.FlatEnums, filePath);
        OnSeparateEnumsWritten(filePath);
    }

    private void WriteSchemaContent<TBufferWriter>(
        ref Utf8StringWriter<TBufferWriter> writer,
        FileWriteContext context,
        HashSet<string> enumTypeNames)
        where TBufferWriter : IBufferWriter<byte>
    {
        foreach (var include in context.Includes)
            writer.AppendFormat($"include \"{include}\";\n");

        if (context.Includes.Count > 0)
            writer.AppendLine();

        if (!string.IsNullOrEmpty(context.Namespace.FinalNamespace))
            writer.AppendFormat($"namespace {context.Namespace.FinalNamespace};\n\n");

        foreach (var flatEnum in context.Enums)
        {
            WriteTableEnum(ref writer, flatEnum);
            writer.AppendLine();
        }

        foreach (var flatTable in context.Tables)
        {
            WriteTable(ref writer, flatTable, context, enumTypeNames);
            writer.AppendLine();
        }
    }

    private void WriteTable<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatTable table,
        FileWriteContext context, HashSet<string> enumTypeNames)
        where TBufferWriter : IBufferWriter<byte>
    {
        writer.AppendFormat($"table {table.TableName} {{\n");

        if (table.NoCreate) writer.AppendLiteral("\t// No Create method\n");

        foreach (var tableField in table.Fields)
            WriteTableField(ref writer, tableField, table, context, enumTypeNames);

        writer.AppendLiteral("}\n");
    }

    private static void WriteTableEnum<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatEnum fEnum)
        where TBufferWriter : IBufferWriter<byte>
    {
        var enumTypeName = TypeHelper.SystemToStringType(fEnum.Type);
        writer.AppendFormat($"enum {fEnum.EnumName} : {enumTypeName} {{\n");

        for (var i = 0; i < fEnum.Fields.Count; i++)
        {
            var field = fEnum.Fields[i];
            var isLast = i == fEnum.Fields.Count - 1;
            writer.AppendFormat($"\t{field.Name} = {field.Value}{(isLast ? "" : ",")}\n");
        }

        writer.AppendLiteral("}\n");
    }

    private void WriteTableField<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatField field,
        FlatTable table, FileWriteContext context, HashSet<string> enumTypeNames)
        where TBufferWriter : IBufferWriter<byte>
    {
        var fieldName = Generation.ForceSnakeCase ? CamelToSnake(field.Name) : field.Name;
        var fieldType = TypeHelper.SystemToStringType(field.Type);
        fieldType = ResolveFieldType(fieldType, field, table, context, enumTypeNames);

        if (ShouldEscapeFieldName(fieldName, table.TableName, fieldType, enumTypeNames))
            fieldName += "_";

        if (field.IsArray) fieldType = $"[{fieldType}]";

        writer.AppendFormat($"\t{fieldName}: {fieldType}; // index 0x{field.Offset:X}\n");
    }

    [GeneratedRegex(@"(([a-z])(?=[A-Z][a-zA-Z])|([A-Z])(?=[A-Z][a-z]))")]
    private static partial Regex CamelToSnakeRegex();
}