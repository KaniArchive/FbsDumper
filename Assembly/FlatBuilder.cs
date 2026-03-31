using dnlib.DotNet;
using FbsDumper.Instructions;
using ZLinq;

namespace FbsDumper.Assembly;

public class FlatBuilder
{
    public readonly long EndObject;
    public readonly Dictionary<long, MethodDef> Methods;
    public readonly long StartObject;

    public FlatBuilder(ModuleDef flatBuffersDllModule)
    {
        var flatBufferBuilderType = flatBuffersDllModule.Find("FlatBuffers.FlatBufferBuilder", false)
                                    ?? throw new InvalidOperationException("Failed to find FlatBufferBuilder.");

        var methodsWithRva = flatBufferBuilderType.Methods
            .AsValueEnumerable()
            .Select(method => new { Method = method, Rva = InstructionsParser.GetMethodRva(method) })
            .Where(x => x.Rva != 0)
            .ToArray();

        Methods = methodsWithRva
            .AsValueEnumerable()
            .GroupBy(x => x.Rva)
            .Select(g => g.First())
            .ToDictionary(x => x.Rva, x => x.Method);

        StartObject = methodsWithRva.AsValueEnumerable().First(x => x.Method.Name == "StartObject").Rva;
        EndObject = methodsWithRva.AsValueEnumerable().First(x => x.Method.Name == "EndObject").Rva;
    }
}