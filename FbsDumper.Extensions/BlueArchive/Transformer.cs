using FbsDumper.Assembly;
using ZLinq;

namespace FbsDumper.Extensions.BlueArchive;

public static class Transformer
{
    public static void Transform(FlatSchema schema)
    {
        foreach (var field in schema.FlatTables
                     .AsValueEnumerable()
                     .Where(table => table.Metadata.TryGetValue("HasEncryption", out var v) && v is true)
                     .SelectMany(table => table.Fields)
                     .Where(field => field.Type.FullName != "System.Boolean"))
            field.Name += " (encrypted)";
    }
}