using dnlib.DotNet;
using FbsDumper.Assembly;

namespace FbsDumper.Extensions.BlueArchive;

public static class Transformer
{
    public static void Transform(FlatTable table, TypeDef typeDef)
    {
        table.Metadata["HasEncryption"] = typeDef.Fields.Any(f =>
            f.IsPublic && f.IsStatic &&
            f.Name == "TableKey" &&
            f.FieldType.FullName == "System.Byte[]");
    }

    public static void Transform(FlatSchema schema) => _ = schema;
}
