namespace FbsDumper.Instructions;

internal interface IInstructionAnalyzer
{
    List<InstructionsAnalyzer.CallInfo> AnalyzeCalls(List<InstructionWithAddress> instructions);
}