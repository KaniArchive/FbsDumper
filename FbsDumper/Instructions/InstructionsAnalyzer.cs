using FbsDumper.Instructions.Analyzers;

namespace FbsDumper.Instructions;

internal abstract class InstructionsAnalyzer
{
    public static IInstructionAnalyzer GetAnalyzer(Architecture architecture) =>
        architecture switch
        {
            Architecture.Arm64 => new ArmAnalyzer(),
            Architecture.X86 => new X86Analyzer(),
            _ => throw new ArgumentException($"Unsupported architecture: {architecture}")
        };

    public class CallInfo
    {
        public ulong Address;
        public int? ArgIndex;
        public Dictionary<string, string> Args = [];
        public string? ArgSource;
        public string? EdxValue;
        public string? Target;
    }
}