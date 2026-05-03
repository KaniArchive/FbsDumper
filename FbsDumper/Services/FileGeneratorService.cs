using FbsDumper.Assembly;
using FbsDumper.Context;

namespace FbsDumper.Services;

public static class FileGeneratorService
{
    public static void Write(FlatSchema schema, FileGenerationContext generation)
    {
        SchemaGeneratorServiceBase generator = generation.IsSplitMode
            ? new SplitGeneratorService(generation)
            : new SingleGeneratorService(generation);

        generator.Generate(schema);
    }

    public static void WriteSingleFile(FlatSchema schema, FileGenerationContext generation)
    {
        var normalized = generation with { IsSplitMode = false };
        Write(schema, normalized);
    }

    public static void WriteSplitFiles(FlatSchema schema, FileGenerationContext generation)
    {
        var normalized = generation with { IsSplitMode = true };
        Write(schema, normalized);
    }
}
