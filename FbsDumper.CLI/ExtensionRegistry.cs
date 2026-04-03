using FbsDumper.Context;
using BlueArchive = FbsDumper.Extensions.BlueArchive.Extension;

namespace FbsDumper.CLI;

internal static class ExtensionRegistry
{
    private static readonly IExtension[] Extensions =
    [
        new BlueArchive()
    ];

    public static ExtensionContext Build(string[]? names = null)
    {
        var ctx = new ExtensionContext();

        if (names == null || names.Length == 0)
            return ctx;

        foreach (var ext in Extensions.Where(e => names.Contains(e.Name, StringComparer.OrdinalIgnoreCase)))
            ext.Register(ctx);

        return ctx;
    }
}
