using dnlib.DotNet;
using FbsDumper.Assembly;
using FbsDumper.Services;

namespace FbsDumper.Context;

public class ExtensionContext
{
    public Action<FlatTable, TypeDef>? OnTableBuilt { get; set; }
    public Action<FlatSchema>? OnSchemaBuilt { get; set; }
    public Action<FlatEnum, TypeDef>? OnEnumBuilt { get; set; }
    public Func<FileGenerationContext, SchemaGeneratorServiceBase>? CreateGenerator { get; set; }
}