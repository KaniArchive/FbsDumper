using System.Globalization;
using dnlib.DotNet;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Instructions;
using ZLinq;

namespace FbsDumper.Assembly.TypeParsers;

public class ArmTypeParser : FieldParser
{
    public override void ProcessFields(ParserOptionsContext context, ref FlatTable ret, MethodDef createMethod,
        TypeDef targetType)
    {
        var dict = ParseCallsForCreateMethod(context, createMethod, targetType);
        dict = dict.AsValueEnumerable().OrderBy(t => t.Key).ToDictionary();

        foreach (var (key, param) in dict)
            AddField(context, ref ret, targetType, key, param);
    }

    private static Dictionary<int, Parameter> ParseCallsForCreateMethod(ParserOptionsContext context,
        MethodDef createMethod, TypeDef targetType)
    {
        Dictionary<int, Parameter> ret = [];
        Dictionary<long, MethodDef> typeMethods = [];

        foreach (var method in targetType.Methods)
        {
            var rva = InstructionsParser.GetMethodRva(method);
            typeMethods.Add(rva, method);
        }

        var calls = TypeHelper.GetAnalyzedCalls(context, createMethod);

        var hasStarted = false;
        var max = 0;
        var cur = 0;

        var endMethodRva = TypeHelper.GetEndMethodRva(targetType);

        foreach (var call in calls)
        {
            if (string.IsNullOrEmpty(call.Target))
            {
                Log.Warning($"Empty call target found at address 0x{call.Address:X}");
                continue;
            }

            if (!TypeHelper.TryParseTarget(call.Target, out var target))
            {
                Log.Warning($"Failed to parse call target '{call.Target}' at address 0x{call.Address:X}");
                continue;
            }

            switch (target)
            {
                case var _ when target == context.FlatBufferBuilder!.StartObject:
                    hasStarted = true;

                    var cnt = ParseArgument(call, "w1");

                    max = cnt;

                    Log.Debug($"Has started, instance will have {cnt} fields");
                    break;

                case var _ when target == context.FlatBufferBuilder.EndObject:
                case var _ when target == endMethodRva:
                    return ret;

                default:
                    if (!hasStarted)
                        Log.Global?.LogSkippingCall((ulong)target, "StartObject hasn't been called yet");

                    if (!typeMethods.TryGetValue(target, out var method))
                    {
                        Log.Global?.LogSkippingCall((ulong)target, $"it's not part of the {targetType.FullName}");
                        continue;
                    }

                    if (cur >= max)
                    {
                        Log.Global?.LogSkippingCall((ulong)target, "max amount of fields has been reached");
                        continue;
                    }

                    var index = ParseCallsForAddMethod(context, method);
                    ret.Add(index, createMethod.Parameters[index + 1]);
                    cur += 1;
                    break;
            }
        }

        return ret;
    }

    private static int ParseCallsForAddMethod(ParserOptionsContext context, MethodDef createMethod)
    {
        var calls = TypeHelper.GetAnalyzedCalls(context, createMethod);
        var call = calls.First(m =>
        {
            if (string.IsNullOrEmpty(m.Target)) return false;

            return TypeHelper.TryParseTarget(m.Target, out var target) &&
                   context.FlatBufferBuilder!.Methods.ContainsKey(target);
        });

        var cnt = ParseArgument(call, "w1");

        Log.Debug($"Index is {cnt}");
        return cnt;
    }

    private static int ParseArgument(InstructionsAnalyzer.CallInfo call, string argName)
    {
        if (!call.Args.TryGetValue(argName, out var arg) || !arg.StartsWith('#'))
            return 0;

        var argValue = arg[1..];
        return int.TryParse(argValue, NumberStyles.Integer, null, out var cnt) ? cnt : 0;
    }
}
