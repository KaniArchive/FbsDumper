using System.Globalization;
using dnlib.DotNet;
using FbsDumper.Context;
using FbsDumper.Helpers;
using FbsDumper.Instructions;
using ZLinq;

namespace FbsDumper.Assembly.TypeParsers;

internal class X86TypeParser : FieldParser
{
    public override void ProcessFields(ParserOptionsContext context, ref FlatTable ret, MethodDef createMethod,
        TypeDef targetType)
    {
        Dictionary<int, (Parameter param, MethodDef method)> dict;

        try
        {
            dict = ParseCallsForCreateMethod(context, createMethod, targetType);
        }
        catch (Exception)
        {
            ForceProcessFields(context, ref ret, createMethod, targetType);
            return;
        }

        dict = dict.AsValueEnumerable().OrderBy(t => t.Key).ToDictionary();

        foreach (var (key, (param, method)) in dict)
            AddField(context, ref ret, targetType, key, param, method.Name.String);
    }

    private static Dictionary<int, (Parameter param, MethodDef method)> ParseCallsForCreateMethod(
        ParserOptionsContext context, MethodDef createMethod, TypeDef targetType)
    {
        Dictionary<int, (Parameter, MethodDef)> ret = [];
        Dictionary<long, MethodDef> typeMethods = [];
        HashSet<int> seenParameterIndices = [];

        foreach (var method in TypeHelper.ResolveTypeDef(createMethod.Parameters[0].Type).Methods)
        {
            var rva = InstructionsParser.GetMethodRva(method);
            typeMethods.Add(rva, method);
        }

        var calls = TypeHelper.GetAnalyzedCalls(context, createMethod);

        var hasStarted = false;
        var max = 0;
        var cur = 0;
        var fieldIndex = 0;

        var endMethodRva = TypeHelper.GetEndMethodRva(targetType);

        if (calls.All(c => c.ArgIndex is null or 0)) return ret;

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
                    max = ParseEdxValue(call);

                    Log.Debug($"Has started, instance will have {max} fields");
                    break;

                case var _ when target == context.FlatBufferBuilder.EndObject:
                case var _ when target == endMethodRva:
                    return ret;

                default:
                    if (!hasStarted)
                    {
                        Log.Global?.LogSkippingCall((ulong)target, "StartObject hasn't been called yet");
                        continue;
                    }

                    if (!typeMethods.TryGetValue(target, out var resolvedMethod))
                    {
                        Log.Global?.LogSkippingCall((ulong)target, $"it's not part of the {targetType.FullName}");
                        continue;
                    }

                    if (cur >= max)
                    {
                        Log.Global?.LogSkippingCall((ulong)target, "max amount of fields has been reached");
                        continue;
                    }

                    var paramIndex = (int)call.ArgIndex! - 1;
                    var parameter = createMethod.Parameters[paramIndex];

                    if (parameter.Name == "builder")
                    {
                        Log.Debug($"Skipping builder parameter '{parameter.Name}' at index {paramIndex}");
                        continue;
                    }

                    if (seenParameterIndices.Contains(paramIndex))
                    {
                        Log.Debug($"Skipping duplicate parameter at index {paramIndex}");
                        continue;
                    }

                    // Store both the parameter and the resolved Add* method for name extraction
                    ret.Add(fieldIndex, (parameter, resolvedMethod));
                    seenParameterIndices.Add(paramIndex);
                    fieldIndex++;
                    cur += 1;
                    break;
            }
        }

        return ret;
    }

    private static int ParseEdxValue(InstructionsAnalyzer.CallInfo call)
    {
        if (call.EdxValue == null) return 0;

        var edxValue = call.EdxValue;
        return edxValue.StartsWith("0x", StringComparison.Ordinal)
            ? int.Parse(edxValue[2..], NumberStyles.HexNumber)
            : int.Parse(edxValue, NumberStyles.Integer);
    }
}