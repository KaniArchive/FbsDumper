using FbsDumper.Context;

namespace FbsDumper.Extensions.BlueArchive;

public class SingleGeneratorService(FileGenerationContext generation) : Services.SingleGeneratorService(generation)
{
    protected override string ResolveFieldType(string typeName, FieldWriteContext field) => typeName;
}

public class SplitGeneratorService(FileGenerationContext generation) : Services.SplitGeneratorService(generation)
{
    protected override string ResolveFieldType(string typeName, FieldWriteContext field) => typeName;
}