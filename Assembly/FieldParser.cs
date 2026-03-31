using dnlib.DotNet;
using FbsDumper.CLI;
using ZLinq;

namespace FbsDumper.Assembly;

public static class FieldParser
{
    public static TypeDef ProcessOffsets(TypeDef targetType, TypeDef fieldType, FlatField field, string fieldName,
        ref TypeSig fieldTypeSig)
    {
        switch (fieldType.FullName)
        {
            case "FlatBuffers.StringOffset":
                field.Type = TypeHelper.GetStringType(targetType);
                field.Name = fieldName.EndsWith("Offset", StringComparison.Ordinal)
                    ? new string([.. fieldName.SkipLast("Offset".Length)])
                    : fieldName;
                field.Name = TypeHelper.CleanFieldName(field.Name);
                break;

            case "FlatBuffers.VectorOffset":
            case "FlatBuffers.Offset":
                var newFieldName = fieldName.EndsWith("Offset", StringComparison.Ordinal)
                    ? new string([.. fieldName.SkipLast("Offset".Length)])
                    : fieldName;
                newFieldName = TypeHelper.CleanFieldName(newFieldName);

                var method = targetType.Methods
                    .AsValueEnumerable()
                    .Where(m => string.Equals(m.Name.String, newFieldName, StringComparison.CurrentCultureIgnoreCase))
                    .OrderByDescending(m => m.IsPublic)
                    .First();

                var typeDefinition = TypeHelper.ResolveTypeDef(method.ReturnType);

                field.IsArray = fieldType.FullName == "FlatBuffers.VectorOffset";

                fieldType = typeDefinition;
                fieldTypeSig = method.ReturnType;

                field.Type = TypeHelper.ToFlatTypeInfo(typeDefinition);
                field.Name = method.Name;
                break;
        }

        return fieldType;
    }

    private static TypeSig ProcessOffsetsByMethods(TypeDef targetType, TypeDef fieldType, FlatField field,
        string fieldName, TypeSig fieldTypeSig, MethodDef method)
    {
        switch (fieldType.FullName)
        {
            case "FlatBuffers.StringOffset":
                field.Type = TypeHelper.GetStringType(targetType);
                field.Name = fieldName.EndsWith("Offset", StringComparison.Ordinal)
                    ? new string([.. fieldName.SkipLast("Offset".Length)])
                    : fieldName;
                break;
            case "FlatBuffers.VectorOffset":
            case "FlatBuffers.Offset":
                var newFieldName = fieldName.EndsWith("Offset", StringComparison.Ordinal)
                    ? new string([.. fieldName.SkipLast("Offset".Length)])
                    : fieldName;
                newFieldName = TypeHelper.CleanFieldName(newFieldName);

                if (fieldType.FullName == "FlatBuffers.VectorOffset")
                {
                    var startMethod = targetType.Methods.First(m => m.Name == $"Start{newFieldName}Vector");
                    fieldType = TypeHelper.ResolveTypeDef(startMethod.Parameters[1].Type);
                    field.IsArray = true;
                }

                fieldTypeSig = fieldType.ToTypeSig();
                field.Type = TypeHelper.ToFlatTypeInfo(fieldType);
                field.Name = method.Name;

                break;
        }

        return fieldTypeSig;
    }

    public static void ForceProcessFields(ref FlatTable ret, MethodDef createMethod, TypeDef targetType)
    {
        foreach (var (param, offset) in createMethod.Parameters.AsValueEnumerable().Skip(1)
                     .Select((p, i) => (p, i + 1)))
        {
            var fieldType = TypeHelper.ResolveTypeDef(param.Type);
            var fieldTypeSig = param.Type;
            var fieldName = param.Name;

            fieldTypeSig = ExtractGeneric(fieldTypeSig, ref fieldType);

            FlatField field = new(TypeHelper.ToFlatTypeInfo(fieldType), TypeHelper.CleanFieldName(fieldName))
            {
                Offset = offset
            };

            fieldType = ProcessOffsets(targetType, fieldType, field, fieldName, ref fieldTypeSig);
            fieldType = SetGeneric(fieldTypeSig, fieldType, field);

            SaveEnum(field, fieldType);

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
            var fieldName = param.Name;

            fieldTypeSig = ExtractGeneric(fieldTypeSig, ref fieldType);
            FlatField field = new(TypeHelper.ToFlatTypeInfo(fieldType), fieldName);

            fieldTypeSig = ProcessOffsetsByMethods(targetType, fieldType, field, fieldName, fieldTypeSig, method);
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

    public static void SaveEnum(FlatField field, TypeDef fieldType)
    {
        if (field.Type.IsEnum && !Parser.FlatEnumsToAdd.Contains(fieldType))
            Parser.FlatEnumsToAdd.Add(fieldType);
    }
}