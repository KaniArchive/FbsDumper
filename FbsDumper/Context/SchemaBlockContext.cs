using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct SchemaBlockContext(
    NamespaceContext Namespace,
    IReadOnlyList<FlatTable> Tables,
    IReadOnlyList<FlatEnum> Enums)
{
    public bool IsEmpty => Tables.Count == 0 && Enums.Count == 0;
}
