using FbsDumper.Context;

namespace FbsDumper.Extensions.BlueArchive;

public class Extension : IExtension
{
    public string Name => "BlueArchive";
    public void Register(ExtensionContext context)
    {
        context.OnTableBuilt = Transformer.Transform;
        context.OnSchemaBuilt = Transformer.Transform;

        context.CreateGenerator = gen => gen.IsSplitMode
            ? new SplitGeneratorService(gen)
            : new SingleGeneratorService(gen);
    }
}
