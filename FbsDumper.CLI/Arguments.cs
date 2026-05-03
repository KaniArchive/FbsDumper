using FbsDumper.Assembly;

namespace FbsDumper.CLI;

public static class Args
{
    /// <summary>
    ///     FlatBuffer Schema Dumper
    /// </summary>
    /// <param name="dummyDll">-d, Specifies the dummy DLL directory.</param>
    /// <param name="gameAssembly">-a, Specifies the path to libil2cpp.so (ARM) or GameAssembly.dll (x86/x64). Leave empty to skip assembly analysis.</param>
    /// <param name="namespaceToLookFor">-nl, Specifies the namespace to look for</param>
    /// <param name="outputFile">-o, Specifies the output file or directory (when using --split).</param>
    /// <param name="namespace">-n, Specifies the flatdata namespace</param>
    /// <param name="split">-sp, Split output into one .fbs file per IL namespace, organized in the output directory.</param>
    /// <param name="enumOut">-eo, How to handle enums: Inline (default), Separate (single enums.fbs), Omit (skip enums).</param>
    /// <param name="forceSnakeCase">-s, Force snake case.</param>
    /// <param name="force">-f, Force processing using Add methods when no Create method exists.</param>
    /// <param name="mxMode">--mx, Enable built-in MX schema tweaks such as encrypted/no-create output annotations.</param>
    /// <param name="skipDuplicates">-sd, Disable namespace breaking. Keep only the first occurrence of duplicate short names.</param>
    /// <param name="verbose">-v, Enable verbose debug logging.</param>
    /// <param name="suppressWarnings">-sw, Suppress warning messages.</param>
    public static void Run(
        string dummyDll,
        string gameAssembly = "",
        string? namespaceToLookFor = null,
        string outputFile = "",
        string @namespace = "",
        bool split = false,
        EnumOut enumOut = EnumOut.Inline,
        bool forceSnakeCase = false,
        bool force = false,
        bool mxMode = false,
        bool skipDuplicates = false,
        bool verbose = false,
        bool suppressWarnings = false) =>
        Parser.Execute(dummyDll, gameAssembly, namespaceToLookFor, outputFile, @namespace, split, enumOut,
            forceSnakeCase, force, mxMode, skipDuplicates, verbose,
            suppressWarnings);
}
