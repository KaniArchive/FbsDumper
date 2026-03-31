namespace FbsDumper.Instructions.Analyzers;

internal class ArmAnalyzer : IInstructionAnalyzer
{
    public List<InstructionsAnalyzer.CallInfo> AnalyzeCalls(List<InstructionWithAddress> instructions)
    {
        var result = new List<InstructionsAnalyzer.CallInfo>();
        var regState = new Dictionary<string, string>();

        foreach (var instr in instructions)
        {
            if (instr.Instruction == null) continue;

            var mnemonic = instr.Mnemonic;
            var operands = ParseArmOperands(instr.Operand);

            ProcessArmInstruction(mnemonic, operands, instr, regState, result);
        }

        return result;
    }

    private static string[] ParseArmOperands(string? operandString) =>
        string.IsNullOrEmpty(operandString)
            ? []
            : operandString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void ProcessArmInstruction(string mnemonic, string[] operands, InstructionWithAddress instr,
        Dictionary<string, string> regState, List<InstructionsAnalyzer.CallInfo> result)
    {
        switch (mnemonic)
        {
            case "mov":
            case "movz":
                ProcessArmMoveInstruction(operands, regState);
                break;
            case "movk":
            case "movn":
                ProcessArmMoveVariantInstruction(operands, mnemonic, regState);
                break;
            case "bl":
            case "b":
                ProcessArmBranchInstruction(instr, regState, result);
                break;
            default:
                ProcessArmOtherInstruction(mnemonic);
                break;
        }
    }

    private static void ProcessArmMoveInstruction(string[] operands, Dictionary<string, string> regState)
    {
        if (operands.Length == 2)
            regState[operands[0]] = operands[1];
    }

    private static void ProcessArmMoveVariantInstruction(string[] operands, string mnemonic,
        Dictionary<string, string> regState)
    {
        if (operands.Length >= 1)
            regState[operands[0]] = $"<{mnemonic}>";
    }

    private static void ProcessArmBranchInstruction(InstructionWithAddress instr, Dictionary<string, string> regState,
        List<InstructionsAnalyzer.CallInfo> result)
    {
        var targetAddress = CalculateArmTarget(instr);

        var call = new InstructionsAnalyzer.CallInfo
        {
            Address = instr.Address,
            Target = targetAddress.HasValue ? $"0x{targetAddress.Value:X}" : "<unknown>",
            Args = []
        };

        PopulateArmCallArguments(call, regState);
        result.Add(call);
    }

    private static void PopulateArmCallArguments(InstructionsAnalyzer.CallInfo call,
        Dictionary<string, string> regState)
    {
        for (var i = 0; i <= 7; i++)
        {
            var xReg = $"x{i}";
            var wReg = $"w{i}";

            if (regState.TryGetValue(xReg, out var value))
                call.Args[xReg] = value;
            else if (regState.TryGetValue(wReg, out var value2))
                call.Args[wReg] = value2;
        }
    }

    private static void ProcessArmOtherInstruction(string mnemonic)
    {
        if (mnemonic == "cbz" || mnemonic == "cmp" || mnemonic.StartsWith('b'))
        {
            // Not Implemented
        }
    }

    private static ulong? CalculateArmTarget(InstructionWithAddress instr)
    {
        var operandString = instr.Operand;
        if (string.IsNullOrEmpty(operandString)) return null;
        var ops = operandString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ops.Length <= 0 || !ops[0].StartsWith('#')) return null;
        var offsetStr = ops[0][1..];
        if (long.TryParse(offsetStr, out var offset)) return instr.Address + (ulong)offset;

        return null;
    }
}