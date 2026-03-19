using System.Buffers;
using System.Text.RegularExpressions;
using FbsDumper.Assembly;
using FbsDumper.Helpers;
using Utf8StringInterpolation;
using ZLinq;

namespace FbsDumper.Services;

public static partial class FileGeneratorService
{
    public static void WriteSingleFile(FlatSchema schema, string outputFile, string? customNamespace,
        EnumOut enumOut, bool forceSnakeCase)
    {
        using var buffer = Utf8String.CreateWriter(out var stringWriter);
        WriteSchemaContent(ref stringWriter, schema.FlatTables, enumOut == EnumOut.Inline ? schema.FlatEnums : [],
            customNamespace, forceSnakeCase, []);
        stringWriter.Flush();
        File.WriteAllBytes(outputFile, buffer.ToArray());

        if (enumOut != EnumOut.Separate || schema.FlatEnums.Count <= 0) return;
        var enumsFile = Path.Combine(
            Path.GetDirectoryName(outputFile) ?? ".",
            Path.GetFileNameWithoutExtension(outputFile) + ".enums.fbs");
        WriteEnumsFile(schema.FlatEnums, enumsFile, customNamespace);
    }

    public static void WriteSplitFiles(FlatSchema schema, string outputDir, string? customNamespace,
        EnumOut enumOut, bool forceSnakeCase)
    {
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        var tablesByNamespace = schema.FlatTables
            .AsValueEnumerable()
            .GroupBy(t => t.OriginalNamespace)
            .ToArray();

        var schemaNamespaces = new HashSet<string>(schema.FlatTables.AsValueEnumerable().Select(t => t.OriginalNamespace));

        foreach (var group in tablesByNamespace)
        {
            var tables = group.ToList();
            var originalNamespace = group.Key;
            var finalNamespace = BuildFinalNamespace(originalNamespace, customNamespace);
            var fileName = string.IsNullOrEmpty(finalNamespace)
                ? "tables.fbs"
                : $"{finalNamespace}.fbs";
            var filePath = Path.Combine(outputDir, fileName);

            IEnumerable<FlatEnum> inlineEnums = enumOut == EnumOut.Inline
                ? schema.FlatEnums.AsValueEnumerable()
                    .Where(e => e.OriginalNamespace == originalNamespace)
                    .ToArray()
                : [];

            var includes = BuildIncludes(tables, originalNamespace, customNamespace, schemaNamespaces, enumOut,
                schema.FlatEnums.Count > 0);

            using var buffer = Utf8String.CreateWriter(out var stringWriter);
            WriteSchemaContent(ref stringWriter, tables, inlineEnums.ToList(), finalNamespace, forceSnakeCase, includes);
            stringWriter.Flush();
            File.WriteAllBytes(filePath, buffer.ToArray());

            Log.Info($"Written: {fileName}");
        }

        if (enumOut != EnumOut.Separate || schema.FlatEnums.Count <= 0) return;
        var enumsPath = Path.Combine(outputDir, "enums.fbs");
        WriteEnumsFile(schema.FlatEnums, enumsPath, customNamespace);
        Log.Info("Written: enums.fbs");
    }

    private static List<string> BuildIncludes(List<FlatTable> tables, string currentNamespace, string? customNamespace,
        HashSet<string> schemaNamespaces, EnumOut enumOut, bool hasEnums)
    {
        var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (enumOut == EnumOut.Separate && hasEnums)
            includes.Add("enums.fbs");

        foreach (var table in tables)
        {
            foreach (var field in table.Fields)
            {
                var dependencyNamespace = field.Type.Namespace;
                if (string.IsNullOrEmpty(dependencyNamespace) || dependencyNamespace == currentNamespace)
                    continue;

                if (!schemaNamespaces.Contains(dependencyNamespace))
                    continue;

                var dependencyFinalNamespace = BuildFinalNamespace(dependencyNamespace, customNamespace);
                var dependencyFileName = string.IsNullOrEmpty(dependencyFinalNamespace)
                    ? "tables.fbs"
                    : $"{dependencyFinalNamespace}.fbs";

                includes.Add(dependencyFileName);
            }
        }

        return [.. includes.AsValueEnumerable().OrderBy(x => x)];
    }

    private static void WriteEnumsFile(List<FlatEnum> enums, string filePath, string? customNamespace)
    {
        using var buffer = Utf8String.CreateWriter(out var stringWriter);
        if (!string.IsNullOrEmpty(customNamespace))
            stringWriter.AppendFormat($"namespace {customNamespace};\n\n");
        foreach (var flatEnum in enums)
        {
            WriteTableEnum(ref stringWriter, flatEnum);
            stringWriter.AppendLine();
        }

        stringWriter.Flush();
        File.WriteAllBytes(filePath, buffer.ToArray());
    }

    private static void WriteSchemaContent<TBufferWriter>(
        ref Utf8StringWriter<TBufferWriter> writer,
        List<FlatTable> tables,
        IEnumerable<FlatEnum> enums,
        string? namespaceName,
        bool forceSnakeCase,
        List<string> includes)
        where TBufferWriter : IBufferWriter<byte>
    {
        foreach (var include in includes)
            writer.AppendFormat($"include \"{include}\";\n");

        if (includes.Count > 0)
            writer.AppendLine();

        if (!string.IsNullOrEmpty(namespaceName))
            writer.AppendFormat($"namespace {namespaceName};\n\n");

        foreach (var flatEnum in enums)
        {
            WriteTableEnum(ref writer, flatEnum);
            writer.AppendLine();
        }

        foreach (var flatTable in tables)
        {
            WriteTable(ref writer, flatTable, forceSnakeCase);
            writer.AppendLine();
        }
    }

    private static void WriteTable<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatTable table,
        bool forceSnakeCase)
        where TBufferWriter : IBufferWriter<byte>
    {
        writer.AppendFormat($"table {table.TableName} {{\n");

        if (table.NoCreate) writer.AppendLiteral("\t// No Create method\n");

        foreach (var tableField in table.Fields)
            WriteTableField(ref writer, tableField, forceSnakeCase);

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

    private static void WriteTableField<TBufferWriter>(ref Utf8StringWriter<TBufferWriter> writer, FlatField field,
        bool forceSnakeCase)
        where TBufferWriter : IBufferWriter<byte>
    {
        var fieldName = forceSnakeCase ? CamelToSnake(field.Name) : field.Name;
        var fieldType = TypeHelper.SystemToStringType(field.Type);

        if (field.IsArray) fieldType = $"[{fieldType}]";

        writer.AppendFormat($"\t{fieldName}: {fieldType}; // index 0x{field.Offset:X}\n");
    }

    private static string CamelToSnake(string camelStr)
    {
        var isAllUppercase = camelStr.All(char.IsUpper);
        if (string.IsNullOrEmpty(camelStr) || isAllUppercase)
            return camelStr;
        return CamelToSnakeRegex().Replace(camelStr, "$1_").ToLower();
    }

    private static string BuildFinalNamespace(string originalNamespace, string? customNamespace)
    {
        if (string.IsNullOrEmpty(customNamespace))
            return originalNamespace;
        if (string.IsNullOrEmpty(originalNamespace))
            return customNamespace;
        return $"{customNamespace}.{originalNamespace}";
    }

    [GeneratedRegex(@"(([a-z])(?=[A-Z][a-zA-Z])|([A-Z])(?=[A-Z][a-z]))")]
    private static partial Regex CamelToSnakeRegex();
}
