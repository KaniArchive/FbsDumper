using FbsDumper.Assembly;
using FbsDumper.Context;

namespace FbsDumper.Services;

internal readonly record struct SchemaBlock(
    NamespaceContext Namespace,
    IReadOnlyList<FlatTable> Tables,
    IReadOnlyList<FlatEnum> Enums)
{
    public bool IsEmpty => Tables.Count == 0 && Enums.Count == 0;
}
