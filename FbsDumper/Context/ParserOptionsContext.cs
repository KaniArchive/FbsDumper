using dnlib.DotNet;
using FbsDumper.Assembly;
using FbsDumper.Instructions;

namespace FbsDumper.Context;

public sealed class ParserOptionsContext(
    string dummyDllPath,
    string gameAssemblyPath,
    string? namespaceToLookFor,
    string outputFile,
    string? customNamespace,
    bool split,
    EnumOut enumOut,
    bool forceSnakeCase,
    bool force,
    bool skipDuplicates,
    bool verbose,
    bool suppressWarnings)
{
    public string DummyDllPath { get; } = dummyDllPath;
    public string GameAssemblyPath { get; } = gameAssemblyPath;
    public string? NamespaceToLookFor { get; } = namespaceToLookFor;
    public string OutputFile { get; } = outputFile;
    public string? CustomNamespace { get; } = customNamespace;
    public bool Split { get; } = split;
    public EnumOut EnumOut { get; } = enumOut;
    public bool ForceSnakeCase { get; } = forceSnakeCase;
    public bool Force { get; } = force;
    public bool SkipDuplicates { get; } = skipDuplicates;
    public bool Verbose { get; } = verbose;
    public bool SuppressWarnings { get; } = suppressWarnings;

    public FlatBuilder? FlatBufferBuilder { get; set; }
    public List<TypeDef> FlatEnumsToAdd { get; } = [];
    public bool NoAsmProcessing { get; set; }

    internal InstructionsParser InstructionsParser =>
        field ??= new InstructionsParser(GameAssemblyPath);
}
