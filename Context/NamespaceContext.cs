namespace FbsDumper.Context;

public readonly record struct NamespaceContext(
    string OriginalNamespace,
    string FinalNamespace
)
{
    public static NamespaceContext Build(string originalNamespace, string? customNamespace)
    {
        if (string.IsNullOrEmpty(customNamespace))
            return new NamespaceContext(originalNamespace, originalNamespace);

        if (string.IsNullOrEmpty(originalNamespace))
            return new NamespaceContext(string.Empty, customNamespace);

        return new NamespaceContext(originalNamespace, $"{customNamespace}.{originalNamespace}");
    }
}
