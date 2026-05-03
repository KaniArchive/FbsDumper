using System.Collections.Frozen;
using System.Globalization;
using dnlib.DotNet;
using FbsDumper.Assembly.TypeParsers;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Instructions;
using ZLinq;

namespace FbsDumper.Assembly;

public static class TypeHelper
{
    private static FrozenDictionary<string, string> TypeMap =>
        new Dictionary<string, string>
        {
            ["System.String"] = "string",
            ["System.Int16"] = "short",
            ["System.UInt16"] = "ushort",
            ["System.Int32"] = "int",
            ["System.UInt32"] = "uint",
            ["System.Int64"] = "long",
            ["System.UInt64"] = "ulong",
            ["System.Boolean"] = "bool",
            ["System.Single"] = "float",
            ["System.SByte"] = "int8",
            ["System.Byte"] = "uint8"
        }.ToFrozenDictionary();

    public static string SystemToStringType(FlatTypeInfo field)
    {
        var fullName = field.FullName;
        if (TypeMap.TryGetValue(fullName, out var type)) return type;

        var name = field.Name;
        if (name.StartsWith("System.", StringComparison.Ordinal)) Log.Global.LogUnknownSystemType(name);

        return name;
    }

    public static ITypeParser GetTypeParser(Architecture architecture) =>
        architecture switch
        {
            Architecture.Arm64 => new ArmTypeParser(),
            Architecture.X86 => new X86TypeParser(),
            _ => throw new ArgumentException($"Unsupported architecture: {architecture}")
        };

    public static string TrimAddPrefix(string fieldName)
    {
        if (fieldName.StartsWith("Add", StringComparison.Ordinal) &&
            fieldName.Length > 3 &&
            char.IsUpper(fieldName[3]))
            return fieldName[3..];

        return fieldName;
    }

    public static string CleanUnderscore(string fieldName) => fieldName.Replace("_", "");

    public static Architecture DetectArchitecture(string gameAssemblyPath)
    {
        var instructionsParser = new InstructionsParser(gameAssemblyPath);
        return instructionsParser.Architecture;
    }

    public static TypeDef ResolveTypeDef(TypeSig typeSig) =>
        typeSig.ToTypeDefOrRef()?.ResolveTypeDef()
        ?? throw new InvalidOperationException($"Failed to resolve type '{typeSig.FullName}'.");

    public static FlatTypeInfo ToFlatTypeInfo(TypeDef typeDef) => FlatTypeInfo.FromTypeDef(typeDef);

    public static FlatTypeInfo ToFlatTypeInfo(TypeSig typeSig) => FlatTypeInfo.FromTypeSig(typeSig);

    public static FlatTypeInfo GetStringType(TypeDef targetType) =>
        FlatTypeInfo.FromTypeSig(targetType.Module.CorLibTypes.String);

    public static List<TypeDef> GetAllFlatBufferTypes(ModuleDef? module, string baseTypeName,
        string? namespaceToLookFor, bool skipDuplicates)
    {
        List<TypeDef> ret =
        [
            .. module!.GetTypes().AsValueEnumerable().Where(t =>
                t.HasInterfaces &&
                t.Interfaces.Any(i => i.Interface?.FullName == baseTypeName)
            ).ToArray()
        ];

        if (!string.IsNullOrEmpty(namespaceToLookFor))
            ret =
            [
                .. ret.AsValueEnumerable().Where(t =>
                    string.Equals(t.Namespace.String, namespaceToLookFor, StringComparison.Ordinal)
                ).ToArray()
            ];

        var byName = ret.AsValueEnumerable().GroupBy(t => t.Name.String ?? string.Empty).ToArray();

        if (skipDuplicates)
            return [.. byName.AsValueEnumerable().Select(g => g.First()).ToArray()];

        foreach (var g in byName.AsValueEnumerable().Where(g => g.Count() > 1))
            Log.Warning($"Duplicate type name '{g.Key}' found in multiple namespaces.");

        return ret;
    }

    public static List<T> CollapseDuplicatesByName<T>(IEnumerable<T> items, Func<T, string> nameSelector) =>
    [
        .. items
            .GroupBy(nameSelector, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
    ];

    public static FlatTable TypeToTable(ParserOptionsContext context, ITypeParser typeParser, TypeDef targetType)
    {
        var typeName = targetType.Name.String ?? string.Empty;
        var ret = new FlatTable(typeName, targetType.Namespace.String ?? string.Empty)
        {
            HasEncryption = context.Mx && targetType.Fields.Any(f =>
                f.IsPublic && f.IsStatic &&
                f.Name == "TableKey" &&
                f.FieldType.FullName == "System.Byte[]")
        };

        var createMethod = targetType.Methods.FirstOrDefault(m =>
            m.Name == $"Create{typeName}" &&
            m.Parameters.Count > 1 &&
            m.Parameters.First().Name == "builder" &&
            m is { IsStatic: true, IsPublic: true }
        );

        if (context.NoAsmProcessing)
        {
            if (createMethod == null)
            {
                var result = context.Force
                    ? ProcessWithForceMethod(ref ret, targetType)
                    : ProcessWithoutCreateMethod(ret, targetType);
                return result;
            }

            FieldParser.ForceProcessFields(context, ref ret, createMethod, targetType);
            ApplyCreateNames(ret, createMethod);
            return ret;
        }

        if (createMethod == null)
        {
            var result = ProcessWithoutCreateMethod(ret, targetType);
            return result;
        }

        typeParser.ProcessFields(context, ref ret, createMethod, targetType);
        ApplyCreateNames(ret, createMethod);
        return ret;
    }

    private static FlatTable ProcessWithForceMethod(ref FlatTable ret, TypeDef targetType)
    {
        ret.NoCreate = true;
        FieldParser.ProcessFieldsByMethods(ref ret, targetType);
        return ret;
    }

    private static FlatTable ProcessWithoutCreateMethod(FlatTable ret, TypeDef targetType)
    {
        Log.Warning($"{targetType.FullName} does NOT contain a Create{targetType.Name} function. Fields will be empty");
        ret.NoCreate = true;
        return ret;
    }

    private static void ApplyCreateNames(FlatTable table, MethodDef createMethod)
    {
        var fieldCount = Math.Min(table.Fields.Count, createMethod.Parameters.Count - 1);

        for (var i = 0; i < fieldCount; i++)
            table.Fields[i].Name = GetName(createMethod.Parameters[i + 1]);
    }

    private static string GetName(Parameter parameter)
    {
        var name = UTF8String.ToSystemString(parameter.Name);
        return name.EndsWith("Offset", StringComparison.Ordinal) && IsOffset(parameter.Type)
            ? name[..^"Offset".Length]
            : name;
    }

    private static bool IsOffset(TypeSig typeSig)
    {
        var fullName = typeSig.ToTypeDefOrRef()?.FullName;
        if (string.IsNullOrEmpty(fullName))
            return false;

        return string.Equals(fullName, "FlatBuffers.StringOffset", StringComparison.Ordinal) ||
               fullName.StartsWith("FlatBuffers.VectorOffset", StringComparison.Ordinal) ||
               fullName.StartsWith("FlatBuffers.Offset", StringComparison.Ordinal);
    }

    public static FlatEnum TypeToEnum(ParserOptionsContext context, TypeDef typeDef)
    {
        var retType = FlatTypeInfo.FromTypeSig(typeDef.GetEnumUnderlyingType());
        var ret = new FlatEnum(retType, typeDef.Name.String ?? string.Empty, typeDef.Namespace.String ?? string.Empty);

        foreach (var fieldDef in typeDef.Fields.AsValueEnumerable().Where(f => f.HasConstant))
        {
            var enumField = new FlatEnumField(fieldDef.Name.String ?? string.Empty,
                Convert.ToInt64(fieldDef.Constant.Value));
            ret.Fields.Add(enumField);
        }

        return ret;
    }

    public static bool TryParseTarget(string target, out long result)
    {
        result = 0;

        if (target.StartsWith("0x", StringComparison.Ordinal))
        {
            var targetHex = target[2..];
            return !string.IsNullOrEmpty(targetHex) &&
                   long.TryParse(targetHex, NumberStyles.HexNumber, null, out result);
        }

        if (!target.StartsWith('#')) return false;
        var targetDecimal = target[1..];
        return !string.IsNullOrEmpty(targetDecimal) &&
               long.TryParse(targetDecimal, NumberStyles.Integer, null, out result);
    }

    internal static List<InstructionsAnalyzer.CallInfo> GetAnalyzedCalls(ParserOptionsContext context,
        MethodDef createMethod)
    {
        var instructions = context.InstructionsParser.GetInstructions(createMethod);
        var analyzer = InstructionsAnalyzer.GetAnalyzer(context.InstructionsParser.Architecture);
        return analyzer.AnalyzeCalls(instructions);
    }

    public static long GetEndMethodRva(TypeDef targetType)
    {
        var endMethod = targetType.Methods.First(m => m.Name == $"End{targetType.Name}");
        return InstructionsParser.GetMethodRva(endMethod);
    }
}
