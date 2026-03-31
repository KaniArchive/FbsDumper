using dnlib.DotNet;
using FbsDumper.Context;

namespace FbsDumper.Assembly;

internal interface ITypeParser
{
    void ProcessFields(ParserOptionsContext context, ref FlatTable ret, MethodDef createMethod, TypeDef targetType);
}