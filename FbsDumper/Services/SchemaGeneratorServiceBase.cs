using System.Buffers;
using CaseConverter;
using FbsDumper.Assembly;
using FbsDumper.Context;
using FbsDumper.Helpers;
using Utf8StringInterpolation;
using ZLinq;

namespace FbsDumper.Services;

public abstract class SchemaGeneratorServiceBase(FileGenerationContext generation)
{
    protected FileGenerationContext Generation { get; } = generation;
    private protected SchemaBlockBuilder Blocks { get; } = new(generation);

    public void Generate(FlatSchema schema)
    {
        var context = SchemaWriteContext.Build(schema);
        PrepareOutput();
        GenerateCore(context);
        WriteSeparateEnumsIfNeeded(context);
    }

    protected virtual void PrepareOutput()
    {
    }

    protected abstract void GenerateCore(SchemaWriteContext context);

    protected abstract string GetSeparateEnumsPath();

    protected void WriteSchemaFile(FileWriteContext file, SchemaWriteContext schema) =>
        WriteSchemaFile(file.OutputPath, [file], file.Includes, schema);

    protected void WriteSchemaFile(string outputPath, IReadOnlyList<FileWriteContext> files,
        IReadOnlyList<string> includes, SchemaWriteContext schema)
    {
        using var buffer = Utf8String.CreateWriter(out var stringWriter);

        WriteIncludes(ref stringWriter, includes);

        for (var i = 0; i < files.Count; i++)
        {
            if (i > 0)
                stringWriter.AppendLine();

            WriteSchema(ref stringWriter, files[i], schema);
        }

        stringWriter.Flush();
        File.WriteAllBytes(outputPath, buffer.ToArray());
    }

    protected virtual string ResolveFieldType(string typeName, FieldWriteContext field)
    {
        if (!field.File.QualifyTypes || !field.Schema.Lookup.HasType(typeName))
            return typeName;

        var typeNs = field.Field.Type.Namespace;
        if (typeNs == field.Table.OriginalNamespace)
            return typeName;

        var ns = NamespaceContext.Build(typeNs, Generation.CustomNamespace);
        return string.IsNullOrEmpty(ns.FinalNamespace)
            ? typeName
            : $"{ns.FinalNamespace}.{typeName}";
    }

    private protected static FileWriteContext CreateFile(string outputPath, SchemaBlockContext block,
        IReadOnlyList<string> includes, bool qualifyTypes) =>
        new(outputPath, block.Namespace, block.Tables, block.Enums, includes, qualifyTypes);

    protected static string BuildSchemaFileName(string originalNamespace, string? customNamespace)
    {
        var namespaceContext = NamespaceContext.Build(originalNamespace, customNamespace);
        return string.IsNullOrEmpty(namespaceContext.FinalNamespace)
            ? "tables.fbs"
            : $"{namespaceContext.FinalNamespace}.fbs";
    }

    protected static bool ReferencesEnum(IReadOnlyList<FlatTable> tables, SchemaLookupContext lookup) =>
        tables.Any(t => t.Fields.Any(f => lookup.EnumNames.Contains(f.Type.Name)));

    private void WriteSeparateEnumsIfNeeded(SchemaWriteContext schema)
    {
        if (Generation.EnumOut != EnumOut.Separate || schema.Schema.FlatEnums.Count <= 0)
            return;

        var filePath = GetSeparateEnumsPath();
        var schemaBlock = new SchemaBlockContext(NamespaceContext.Build(string.Empty, Generation.CustomNamespace), [], schema.Schema.FlatEnums);

        IReadOnlyList<FileWriteContext> files = schema.Lookup.HasDuplicates
            ? [CreateFile(filePath, schemaBlock, [], false)]
            : [.. Blocks.BuildEnums(schema).AsValueEnumerable().Select(block => CreateFile(filePath, block, [], false))];

        WriteSchemaFile(filePath, files, [], schema);

        if (Generation.IsSplitMode)
            Log.Info($"Written: {Path.GetFileName(filePath)}");
    }

    protected virtual void WriteIncludes<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer,
        IReadOnlyList<string> includes)
        where TBufferWriter : IBufferWriter<byte>
    {
        foreach (var include in includes)
            writer.AppendFormat($"include \"{include}\";\n");

        if (includes.Count > 0)
            writer.AppendLine();
    }

    protected virtual void WriteSchema<TBufferWriter>(
        ref Utf8StringWriter<TBufferWriter> writer,
        FileWriteContext file,
        SchemaWriteContext schema)
        where TBufferWriter : IBufferWriter<byte>
    {
        if (!string.IsNullOrEmpty(file.Namespace.FinalNamespace))
            writer.AppendFormat($"namespace {file.Namespace.FinalNamespace};\n\n");

        foreach (var fEnum in file.Enums)
        {
            WriteEnum(ref writer, fEnum);
            writer.AppendLine();
        }

        foreach (var table in file.Tables)
        {
            WriteTable(ref writer, table, file, schema);
            writer.AppendLine();
        }
    }

    protected virtual void WriteTable<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatTable table,
        FileWriteContext file, SchemaWriteContext schema)
        where TBufferWriter : IBufferWriter<byte>
    {
        var modifiers = new List<string>(2);
        if (table.HasEncryption)
            modifiers.Add("encrypted");
        if (table.NoCreate)
            modifiers.Add("no_create");

        var tableDecl = modifiers.Count > 0
            ? $"table {table.TableName} ({string.Join(", ", modifiers)})"
            : $"table {table.TableName}";
        writer.AppendFormat($"{tableDecl} {{\n");

        if (table.NoCreate && table.Fields.Count == 0)
            writer.AppendLiteral("\t// No Create method\n");

        foreach (var field in table.Fields)
            WriteField(ref writer, new FieldWriteContext(field, table, file, schema));

        writer.AppendLiteral("}\n");
    }

    protected virtual void WriteEnum<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatEnum fEnum)
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

    protected virtual void WriteField<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FieldWriteContext field)
        where TBufferWriter : IBufferWriter<byte>
    {
        var name = GetFieldName(field);
        var type = GetFieldType(field);

        writer.AppendFormat($"\t{name}: {type}; // index 0x{field.Field.Offset:X}\n");
    }

    protected string GetFieldName(FieldWriteContext field) =>
        Generation.ForceSnakeCase ? field.Field.Name.ToSnakeCase() : field.Field.Name;

    protected string GetFieldType(FieldWriteContext field)
    {
        var type = TypeHelper.SystemToStringType(field.Field.Type);
        type = ResolveFieldType(type, field);

        if (field.Field.IsArray)
            type = $"[{type}]";

        return type;
    }
}
