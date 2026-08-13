using dnlib.DotNet;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Instructions;
using ZLinq;

namespace FbsDumper.Assembly;

public static class SchemaBuilder
{
    private const string FlatBaseType = "FlatBuffers.IFlatbufferObject";

    public static FlatSchema Build(ParserOptionsContext context, string dummyDllPath)
    {
        var moduleContext = ModuleDef.CreateModuleContext();
        var resolver = (AssemblyResolver)moduleContext.AssemblyResolver;
        resolver.EnableTypeDefCache = true;
        resolver.PreSearchPaths.Add(dummyDllPath);

        Log.Info("Reading game assemblies...");

        var candidates = Directory.GetFiles(dummyDllPath, "*.dll")
            .AsValueEnumerable()
            .Where(dll => !Path.GetFileName(dll).Equals("FlatBuffers.dll", StringComparison.OrdinalIgnoreCase))
            .Select(dll =>
            {
                try
                {
                    return ModuleDefMD.Load(dll, moduleContext);
                }
                catch
                {
                    return null;
                }
            })
            .Where(mod => mod != null)
            .Where(mod => mod != null && mod.GetTypes().AsValueEnumerable().Any(t =>
                t.HasInterfaces &&
                t.Interfaces.AsValueEnumerable().Any(i => i.Interface?.FullName == FlatBaseType)))
            .ToList();

        switch (candidates.Count)
        {
            case 0:
                Log.Error($"No DLL implementing {FlatBaseType} found in '{dummyDllPath}'.");
                Log.Shutdown();
                Environment.Exit(1);
                break;
            case > 1 when string.IsNullOrEmpty(context.NamespaceToLookFor):
            {
                Log.Warning("Multiple DLLs contain FlatBuffer types:");
                foreach (var c in candidates)
                    Log.Warning($"  {Path.GetFileName(c?.Location)}");
                Log.Error("Pass --namespace-to-look-for (-nl) to disambiguate.");
                Log.Shutdown();
                Environment.Exit(1);
                break;
            }
        }

        var asm = candidates.Count == 1
            ? candidates[0]
            : candidates.AsValueEnumerable().First(c => c != null && c.GetTypes().AsValueEnumerable().Any(t =>
                string.Equals(t.Namespace.String, context.NamespaceToLookFor, StringComparison.Ordinal)));

        foreach (var c in candidates.Where(c => c != asm))
            c?.Dispose();

        if (!context.NoAsmProcessing)
        {
            var flatBuffersDllPath = Path.Combine(dummyDllPath, "FlatBuffers.dll");
            if (!File.Exists(flatBuffersDllPath))
            {
                Log.Error(
                    $"FlatBuffers.dll not found in '{dummyDllPath}'. Required for assembly analysis. Omit --game-assembly (-a) to skip.");
                Log.Shutdown();
                Environment.Exit(1);
            }

            using var asmFbs = ModuleDefMD.Load(flatBuffersDllPath, moduleContext);
            context.FlatBufferBuilder = new FlatBuilder(asmFbs);
        }

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

        Log.Success($"Found {typeDefs.Count} total");

        FlatSchema schema = new();

        foreach (var typeDef in typeDefs)
        {
            Log.GlobalSuccess.LogDisassembled(typeDef.Name);
            var table = TypeHelper.TypeToTable(context, typeParser, typeDef);
            schema.FlatTables.Add(table);
        }

        Log.Info("Adding enums...");
        foreach (var fEnum in context.FlatEnumsToAdd.AsValueEnumerable().Select(t => TypeHelper.TypeToEnum(context, t)))
            schema.FlatEnums.Add(fEnum);

        if (context.SkipDuplicates)
        {
            var distinctEnums = TypeHelper.CollapseDuplicatesByName(schema.FlatEnums, fEnum => fEnum.EnumName);
            schema.FlatEnums.Clear();
            schema.FlatEnums.AddRange(distinctEnums);
        }

        asm?.Dispose();
        return schema;
    }
}