using FbsDumper.Assembly;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Services;

namespace FbsDumper.CLI;

public static class Parser
{
    public static void Execute(string dummyDll, string gameAssembly, string? namespaceToLookFor, string outputFile,
        string nameSpace, bool split, EnumOut enumOut, bool forceSnakeCase, bool force, bool skipDuplicates,
        bool verbose, bool suppressWarnings)
    {
        if (verbose) Log.EnableDebugLogging();
        Log.SuppressWarnings = suppressWarnings;

        if (string.IsNullOrEmpty(outputFile))
            outputFile = "tables.fbs";

        var context = new ParserOptionsContext(
            dummyDll,
            gameAssembly,
            namespaceToLookFor,
            outputFile,
            string.IsNullOrWhiteSpace(nameSpace) ? null : nameSpace,
            split,
            enumOut,
            forceSnakeCase,
            force,
            skipDuplicates,
            verbose,
            suppressWarnings);

        context.NoAsmProcessing = ValidatePaths(context);

        var schema = SchemaBuilder.Build(context, dummyDll);

        var generation = new FileGenerationContext(
            context.OutputFile,
            context.CustomNamespace,
            context.EnumOut,
            context.ForceSnakeCase,
            context.Split);

        FileGeneratorService.Write(schema, generation);

        Log.Info("Done.");
    }

    private static bool ValidatePaths(ParserOptionsContext context)
    {
        if (!Directory.Exists(context.DummyDllPath))
        {
            Log.Global.LogDummyDirNotFound(context.DummyDllPath);
            Log.Error("Please provide a valid path using -dummydll or -d.");
            Log.Shutdown();
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(context.GameAssemblyPath))
        {
            Log.Info("No game assembly provided. Skipping assembly analysis.");
            return true;
        }

        if (File.Exists(context.GameAssemblyPath))
            return false;

        Log.Global.LogGameAssemblyNotFound(context.GameAssemblyPath);
        Log.Error("Please provide a valid path using -gameassembly or -a.");
        Log.Shutdown();
        Environment.Exit(1);
        return false;
    }
}
