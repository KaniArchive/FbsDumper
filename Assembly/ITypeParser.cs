using Mono.Cecil;

namespace FbsDumper.Assembly;

internal interface ITypeParser
{
    void ProcessFields(ref FlatTable ret, MethodDefinition createMethod, TypeDefinition targetType);
}
