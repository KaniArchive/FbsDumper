namespace FbsDumper.Context;

public interface IExtension
{
    string Name { get; }
    void Register(ExtensionContext context);
}