using dnlib.DotNet;

namespace FbsDumper.Assembly;

public sealed record FlatTypeInfo(string Name, string Namespace, string FullName, bool IsEnum = false)
{
    public static FlatTypeInfo FromTypeDef(TypeDef typeDef) =>
        new(
            typeDef.Name.String ?? string.Empty,
            typeDef.Namespace.String ?? string.Empty,
            typeDef.FullName,
            typeDef.IsEnum);

    public static FlatTypeInfo FromTypeSig(TypeSig typeSig)
    {
        var typeDef = typeSig.ToTypeDefOrRef()?.ResolveTypeDef();
        return typeDef != null
            ? FromTypeDef(typeDef)
            : new FlatTypeInfo(typeSig.TypeName ?? string.Empty, typeSig.Namespace ?? string.Empty, typeSig.FullName);
    }
}