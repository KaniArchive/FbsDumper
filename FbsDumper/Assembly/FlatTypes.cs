using System.Text.Json.Serialization;

namespace FbsDumper.Assembly;

public class FlatSchema
{
    public readonly List<FlatEnum> FlatEnums = [];
    public readonly List<FlatTable> FlatTables = [];
}

public class FlatTable(string tableName, string originalNamespace = "")
{
    public readonly List<FlatField> Fields = [];
    public readonly string OriginalNamespace = originalNamespace;
    public readonly string TableName = tableName;
    public bool NoCreate = false;
    public Dictionary<string, object> Metadata { get; } = [];
}

public class FlatField(FlatTypeInfo type, string name, bool isArray = false)
{
    public bool IsArray = isArray;
    public string Name = name;
    public int Offset;

    [JsonIgnore] public FlatTypeInfo Type = type;
}

public class FlatEnum(FlatTypeInfo valueType, string enumName, string originalNamespace = "")
{
    public readonly string EnumName = enumName;
    public readonly List<FlatEnumField> Fields = [];
    public readonly string OriginalNamespace = originalNamespace;

    [JsonIgnore] public readonly FlatTypeInfo Type = valueType;
}

public class FlatEnumField(string name, long value = 0)
{
    public readonly string Name = name;
    public readonly long Value = value;
}