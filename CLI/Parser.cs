using FbsDumper.Assembly;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Instructions;
using FbsDumper.Services;
using Mono.Cecil;
using ZLinq;

namespace FbsDumper.CLI;

public static class Parser
{
    private static readonly string FlatBaseType = "FlatBuffers.IFlatbufferObject";
    private static string _dummyAssemblyDir = "DummyDll";
    public static string GameAssemblyPath = "libil2cpp.so";
    public static string? NameSpace2LookFor;

    public static FlatBuilder? FlatBufferBuilder;
    public static readonly List<TypeDefinition> FlatEnumsToAdd = [];

    public static bool Force;
    public static bool SkipDuplicates;
    public static bool SuppressWarnings;
    public static bool NoAsmProcessing;

    public static void Execute(string dummyDll, string gameAssembly, string? namespaceToLookFor, string outputFile,
        string nameSpace, bool split, EnumOut enumOut, bool forceSnakeCase, bool force, bool skipDuplicates,
        bool verbose, bool suppressWarnings)
    {
        if (verbose) Log.EnableDebugLogging();

        _dummyAssemblyDir = dummyDll;
        GameAssemblyPath = gameAssembly;
        NameSpace2LookFor = namespaceToLookFor;

        Force = force;
        SkipDuplicates = skipDuplicates;
        SuppressWarnings = suppressWarnings;

        var customNamespace = split ? nameSpace == "FlatData" ? null : nameSpace : nameSpace;

        if (!Directory.Exists(_dummyAssemblyDir))
        {
            Log.Global.LogDummyDirNotFound(_dummyAssemblyDir);
            Log.Error("Please provide a valid path using -dummydll or -d.");
            Log.Shutdown();
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(GameAssemblyPath))
        {
            Log.Info("No game assembly provided. Skipping assembly analysis.");
            NoAsmProcessing = true;
        }
        else if (!File.Exists(GameAssemblyPath))
        {
            Log.Global.LogGameAssemblyNotFound(GameAssemblyPath);
            Log.Error("Please provide a valid path using -gameassembly or -a.");
            Log.Shutdown();
            Environment.Exit(1);
        }

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(_dummyAssemblyDir);
        var readerParameters = new ReaderParameters
        {
            AssemblyResolver = resolver
        };
        Log.Info("Reading game assemblies...");

        var blueArchiveDllPath = Path.Combine(_dummyAssemblyDir, "BlueArchive.dll");
        if (!File.Exists(blueArchiveDllPath))
        {
            Log.Global.LogFileNotFound("BlueArchive.dll", _dummyAssemblyDir);
            Log.Shutdown();
            Environment.Exit(1);
        }

        var asm = AssemblyDefinition.ReadAssembly(blueArchiveDllPath, readerParameters);

        var flatBuffersDllPath = Path.Combine(_dummyAssemblyDir, "FlatBuffers.dll");
        if (!File.Exists(flatBuffersDllPath))
        {
            Log.Global.LogFileNotFound("FlatBuffers.dll", _dummyAssemblyDir);
            Log.Shutdown();
            Environment.Exit(1);
        }

        var asmFbs = AssemblyDefinition.ReadAssembly(flatBuffersDllPath, readerParameters);

        FlatBufferBuilder = new FlatBuilder(asmFbs.MainModule);

        var architecture = NoAsmProcessing ? Architecture.X86 : TypeHelper.DetectArchitecture(GameAssemblyPath);
        var typeParser = TypeHelper.GetTypeParser(architecture);

        Log.Info(NoAsmProcessing ? "Using no assembly analysis mode" : $"Detected architecture: {architecture}");
        Log.Info("Getting a list of types...");

        var typeDefs = TypeHelper.GetAllFlatBufferTypes(asm.MainModule, FlatBaseType);

        FlatSchema schema = new();

        var done = 0;

        foreach (var typeDef in typeDefs)
        {
            Log.Global.LogProgress(done + 1, typeDefs.Count);
            var table = TypeHelper.TypeToTable(typeParser, typeDef);

            schema.FlatTables.Add(table);
            done += 1;
        }

        Log.Info("Adding enums...");
        foreach (var fEnum in FlatEnumsToAdd.AsValueEnumerable().Select(TypeHelper.TypeToEnum))
            schema.FlatEnums.Add(fEnum);

        if (SkipDuplicates)
        {
            var distinctEnums = TypeHelper.CollapseDuplicatesByName(schema.FlatEnums, fEnum => fEnum.EnumName);
            schema.FlatEnums.Clear();
            schema.FlatEnums.AddRange(distinctEnums);
        }

        var generation = new FileGenerationContext(outputFile, customNamespace, enumOut, forceSnakeCase, split);
        FileGeneratorService.Write(schema, generation);

        Log.Info("Done.");
    }
}
