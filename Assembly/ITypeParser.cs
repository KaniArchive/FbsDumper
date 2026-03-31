using FbsDumper.Context;
using dnlib.DotNet;

namespace FbsDumper.Assembly;

internal interface ITypeParser
{
    void ProcessFields(ParserOptionsContext context, ref FlatTable ret, MethodDef createMethod, TypeDef targetType);
}
