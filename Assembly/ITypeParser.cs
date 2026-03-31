using dnlib.DotNet;

namespace FbsDumper.Assembly;

internal interface ITypeParser
{
    void ProcessFields(ref FlatTable ret, MethodDef createMethod, TypeDef targetType);
}
