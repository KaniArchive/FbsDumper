using dnlib.DotNet;
using FbsDumper.Context;
using ZLinq;

namespace FbsDumper.Assembly;

public abstract class FieldParser : ITypeParser
{
    public abstract void ProcessFields(ParserOptionsContext context, ref FlatTable ret, MethodDef createMethod,
        TypeDef targetType);

    protected static void AddField(ParserOptionsContext context, ref FlatTable ret, TypeDef targetType, int offset,
        Parameter param)
    {
        var fieldType = TypeHelper.ResolveTypeDef(param.Type);
        var fieldTypeSig = param.Type;

        var fieldName = UTF8String.ToSystemString(param.Name);

        fieldTypeSig = ExtractGeneric(fieldTypeSig, ref fieldType);

        FlatField field = new(TypeHelper.ToFlatTypeInfo(fieldType), fieldName)
        {
            Offset = offset
        };

        fieldType = ProcessOffsets(targetType, fieldType, field, fieldName, ref fieldTypeSig);
        fieldType = SetGeneric(fieldTypeSig, fieldType, field);

        SaveEnum(context, field, fieldType);
        ret.Fields.Add(field);
    }

    public static TypeDef ProcessOffsets(TypeDef targetType, TypeDef fieldType,
        FlatField field, string fieldName, ref TypeSig fieldTypeSig)
    {
        switch (fieldType.FullName)
        {
            case "FlatBuffers.StringOffset":
                field.Type = TypeHelper.GetStringType(targetType);
                field.Name = GetNames(fieldName).First();
                break;

            case "FlatBuffers.VectorOffset":
            case "FlatBuffers.Offset":
                var accessor = FindAccessor(targetType, fieldName);

                field.IsArray = fieldType.FullName == "FlatBuffers.VectorOffset";

                if (accessor != null)
                {
                    var resolvedType = TypeHelper.ResolveTypeDef(accessor.ReturnType);
                    fieldType = resolvedType;
                    fieldTypeSig = accessor.ReturnType;
                    field.Type = TypeHelper.ToFlatTypeInfo(resolvedType);
                    field.Name = accessor.Name;
                    break;
                }

                if (!field.IsArray) break;

                var vectorTypeSig = FindCreateVectorType(targetType, fieldName);
                if (vectorTypeSig == null) break;

                fieldType = TypeHelper.ResolveTypeDef(vectorTypeSig);
                fieldTypeSig = vectorTypeSig;
                field.Type = TypeHelper.ToFlatTypeInfo(vectorTypeSig);
                field.Name = GetNames(fieldName).Last();
                break;
        }

        return fieldType;
    }

    public static TypeSig ProcessOffsetsByMethods(TypeDef targetType, TypeDef fieldType, FlatField field,
        string fieldName, TypeSig fieldTypeSig, MethodDef method)
    {
        switch (fieldType.FullName)
        {
            case "FlatBuffers.StringOffset":
                field.Type = TypeHelper.GetStringType(targetType);
                field.Name = GetNames(fieldName).First();
                break;
            case "FlatBuffers.VectorOffset":
            case "FlatBuffers.Offset":
                if (fieldType.FullName == "FlatBuffers.VectorOffset")
                {
                    var vectorTypeSig = GetVectorType(targetType, fieldName);
                    fieldType = TypeHelper.ResolveTypeDef(vectorTypeSig);
                    fieldTypeSig = vectorTypeSig;
                    field.IsArray = true;
                }

                if (!field.IsArray)
                    fieldTypeSig = fieldType.ToTypeSig();

                field.Type = TypeHelper.ToFlatTypeInfo(fieldType);
                field.Name = TypeHelper.TrimAddPrefix(method.Name);

                break;
        }

        return fieldTypeSig;
    }

    public static void ForceProcessFields(ParserOptionsContext context, ref FlatTable ret, MethodDef createMethod,
        TypeDef targetType)
    {
        foreach (var (param, offset) in createMethod.Parameters.AsValueEnumerable().Skip(1)
                     .Select((p, i) => (p, i + 1)))
        {
            var fieldType = TypeHelper.ResolveTypeDef(param.Type);
            var fieldTypeSig = param.Type;
            var fieldName = UTF8String.ToSystemString(param.Name);

            fieldTypeSig = ExtractGeneric(fieldTypeSig, ref fieldType);

            FlatField field = new(TypeHelper.ToFlatTypeInfo(fieldType), fieldName)
            {
                Offset = offset
            };

            fieldType = ProcessOffsets(targetType, fieldType, field, fieldName, ref fieldTypeSig);
            fieldType = SetGeneric(fieldTypeSig, fieldType, field);

            SaveEnum(context, field, fieldType);

            ret.Fields.Add(field);
        }
    }

    public static void ProcessFieldsByMethods(ref FlatTable ret, TypeDef targetType)
    {
        foreach (var method in targetType.Methods.Where(m =>
                     m.IsPublic && m.IsStatic && m.Name.StartsWith("Add", StringComparison.Ordinal) &&
                     m.Parameters.Count == 2 && m.Parameters.First().Name == "builder"))
        {
            var param = method.Parameters[1];

            var fieldType = TypeHelper.ResolveTypeDef(param.Type);
            var fieldTypeSig = param.Type;

            var fieldName = TypeHelper.TrimAddPrefix(method.Name.String);

            fieldTypeSig = ExtractGeneric(fieldTypeSig, ref fieldType);
            FlatField field = new(TypeHelper.ToFlatTypeInfo(fieldType), fieldName);

            fieldTypeSig =
                ProcessOffsetsByMethods(targetType, fieldType, field, fieldName, fieldTypeSig, method);
            SetGeneric(fieldTypeSig, fieldType, field);

            ret.Fields.Add(field);
        }
    }

    public static TypeSig ExtractGeneric(TypeSig fieldTypeSig, ref TypeDef fieldType)
    {
        if (fieldTypeSig is not GenericInstSig genericInstance) return fieldTypeSig;
        fieldType = TypeHelper.ResolveTypeDef(genericInstance.GenericArguments.First());
        fieldTypeSig = genericInstance.GenericArguments.First();

        return fieldTypeSig;
    }

    public static TypeDef SetGeneric(TypeSig fieldTypeSig, TypeDef fieldType, FlatField field)
    {
        if (!fieldTypeSig.IsGenericInstanceType) return fieldType;

        var newGenericInstance = (GenericInstSig)fieldTypeSig;
        fieldType = TypeHelper.ResolveTypeDef(newGenericInstance.GenericArguments.First());
        field.Type = TypeHelper.ToFlatTypeInfo(fieldType);

        return fieldType;
    }

    public static void SaveEnum(ParserOptionsContext context, FlatField field, TypeDef fieldType)
    {
        if (field.Type.IsEnum && !context.FlatEnumsToAdd.Contains(fieldType))
            context.FlatEnumsToAdd.Add(fieldType);
    }

    private static MethodDef? FindAccessor(TypeDef targetType, string fieldName) =>
        GetNames(fieldName)
            .AsValueEnumerable()
            .Select(candidate => targetType.Methods.AsValueEnumerable()
                .Where(m => string.Equals(m.Name.String, candidate, StringComparison.OrdinalIgnoreCase) &&
                            !m.Name.String.StartsWith("Add", StringComparison.Ordinal))
                .OrderByDescending(m => m.IsPublic)
                .FirstOrDefault())
            .OfType<MethodDef>()
            .FirstOrDefault();

    private static TypeSig GetVectorType(TypeDef targetType, string fieldName)
    {
        var accessor = FindAccessor(targetType, fieldName);
        if (accessor != null)
            return accessor.ReturnType;

        var createTypeSig = FindCreateVectorType(targetType, fieldName);
        return createTypeSig ??
               throw new InvalidOperationException($"Failed to resolve vector element type for '{fieldName}'.");
    }

    private static TypeSig? FindCreateVectorType(TypeDef targetType, string fieldName)
    {
        foreach (var candidate in GetNames(fieldName))
        {
            var createMethod = targetType.Methods.FirstOrDefault(m => m.Name == $"Create{candidate}Vector");
            if (createMethod == null || createMethod.Parameters.Count < 2)
                continue;

            var dataType = createMethod.Parameters[1].Type;
            if (dataType.Next is not null)
                return dataType.Next;
        }

        return null;
    }

    private static string[] GetNames(string fieldName)
    {
        var originalName = TypeHelper.CleanUnderscore(fieldName);

        if (!fieldName.EndsWith("Offset", StringComparison.Ordinal))
            return [originalName];

        var strippedName = TypeHelper.CleanUnderscore(new string([.. fieldName.SkipLast("Offset".Length)]));
        return string.Equals(strippedName, originalName, StringComparison.Ordinal)
            ? [originalName]
            : [strippedName, originalName];
    }
}
