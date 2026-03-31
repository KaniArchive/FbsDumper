using FbsDumper.Context;

namespace FbsDumper.Extensions.BlueArchive;

public class BlueArchiveExtension : IExtension
{
    public string Name => "BlueArchive";
    public void Register(ExtensionContext context)
    {
        context.OnTableBuilt = (table, typeDef) =>
        {
            if (typeDef.Namespace.String != "FlatData") return;

            table.Metadata["HasEncryption"] = typeDef.Fields.Any(f =>
                f.IsPublic && f.IsStatic &&
                f.Name == "TableKey" &&
                f.FieldType.FullName == "System.Byte[]");
        };

        context.OnSchemaBuilt = Transformer.Transform;
    
        context.CreateGenerator = gen => gen.IsSplitMode
            ? new SplitGeneratorService(gen)
            : new SingleGeneratorService(gen);
    }
}