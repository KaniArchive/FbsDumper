using dnlib.DotNet;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Instructions;
using ZLinq;

namespace FbsDumper.Assembly;

internal static class SchemaBuilder
{
    private const string FlatBaseType = "FlatBuffers.IFlatbufferObject";

    public static FlatSchema Build(ParserOptionsContext context, string dummyDllPath)
    {
        var moduleContext = ModuleDef.CreateModuleContext();
        var resolver = (AssemblyResolver)moduleContext.AssemblyResolver;
        resolver.EnableTypeDefCache = true;
        resolver.PreSearchPaths.Add(dummyDllPath);

        Log.Info("Reading game assemblies...");

        var blueArchiveDllPath = Path.Combine(dummyDllPath, "BlueArchive.dll");
        if (!File.Exists(blueArchiveDllPath))
        {
            Log.Global.LogFileNotFound("BlueArchive.dll", dummyDllPath);
            Log.Shutdown();
            Environment.Exit(1);
        }

        using var asm = ModuleDefMD.Load(blueArchiveDllPath, moduleContext);

        var flatBuffersDllPath = Path.Combine(dummyDllPath, "FlatBuffers.dll");
        if (!File.Exists(flatBuffersDllPath))
        {
            Log.Global.LogFileNotFound("FlatBuffers.dll", dummyDllPath);
            Log.Shutdown();
            Environment.Exit(1);
        }

        using var asmFbs = ModuleDefMD.Load(flatBuffersDllPath, moduleContext);

        context.FlatBufferBuilder = new FlatBuilder(asmFbs);

        var architecture = context.NoAsmProcessing
            ? Architecture.X86
            : TypeHelper.DetectArchitecture(context.GameAssemblyPath);
        var typeParser = TypeHelper.GetTypeParser(architecture);

        Log.Info(context.NoAsmProcessing
            ? "Using no assembly analysis mode"
            : $"Detected architecture: {architecture}");
        Log.Info("Getting a list of types...");

        var typeDefs = TypeHelper.GetAllFlatBufferTypes(
            asm,
            FlatBaseType,
            context.NamespaceToLookFor,
            context.SkipDuplicates);

        FlatSchema schema = new();
        var done = 0;

        foreach (var typeDef in typeDefs)
        {
            Log.Global.LogProgress(done + 1, typeDefs.Count);
            var table = TypeHelper.TypeToTable(context, typeParser, typeDef);
            schema.FlatTables.Add(table);
            done += 1;
        }

        Log.Info("Adding enums...");
        foreach (var fEnum in context.FlatEnumsToAdd.AsValueEnumerable().Select(TypeHelper.TypeToEnum))
            schema.FlatEnums.Add(fEnum);

        if (context.SkipDuplicates)
        {
            var distinctEnums = TypeHelper.CollapseDuplicatesByName(schema.FlatEnums, fEnum => fEnum.EnumName);
            schema.FlatEnums.Clear();
            schema.FlatEnums.AddRange(distinctEnums);
        }

        return schema;
    }
}