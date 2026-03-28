using FbsDumper.Assembly;

namespace FbsDumper.Context;

public readonly record struct FieldWriteContext(
    FlatField Field,
    FlatTable Table,
    FileWriteContext File,
    SchemaWriteContext Schema);