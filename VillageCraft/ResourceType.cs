using System.Text.Json;
using System.Text.Json.Serialization;
namespace VillageCraft;

// Stable internal IDs. Text names are parsed only at TOML, JavaScript and save boundaries.
[Tomlyn.Serialization.TomlConverter(typeof(ResourceTomlConverter))]
[JsonConverter(typeof(ResourceTypeConverter))]
public enum ResourceType : byte
{
    None=0, Emeralds=1, Food=2, Wood=3, Stone=4, Iron=5, Tools=6, Wool=7,
    Eggs=8, Gold=9, Diamonds=10, AncientDebris=11, Goods=12, Leather=14, Milk=15
}
public static class ResourceIds
{
    static readonly Dictionary<string,ResourceType> byName=Enum.GetValues<ResourceType>().Where(r=>r!=ResourceType.None).ToDictionary(r=>r.ToString(),StringComparer.Ordinal);
    static readonly Dictionary<ResourceType,string> names=Enum.GetValues<ResourceType>().ToDictionary(r=>r,r=>r==ResourceType.None?string.Empty:r.ToString());
    public static string Name(ResourceType type)=>names.TryGetValue(type,out var name)?name:throw new InvalidDataException("Invalid resource ID.");
    public static bool TryParse(string? name,out ResourceType resource)
    {
        resource=ResourceType.None;return !string.IsNullOrEmpty(name)&&byName.TryGetValue(name,out resource);
    }
    public static ResourceType Parse(string name,bool optional=false)=>optional&&string.IsNullOrEmpty(name)?ResourceType.None:TryParse(name,out var resource)?resource:throw new InvalidDataException("Unknown resource: "+name);
}
public sealed class ResourceTypeConverter:JsonConverter<ResourceType>
{
    public override ResourceType Read(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>ResourceIds.Parse(reader.GetString()??string.Empty,true);
    public override void Write(Utf8JsonWriter writer,ResourceType value,JsonSerializerOptions options)=>writer.WriteStringValue(ResourceIds.Name(value));
    public override ResourceType ReadAsPropertyName(ref Utf8JsonReader reader,Type type,JsonSerializerOptions options)=>ResourceIds.Parse(reader.GetString()??string.Empty);
    public override void WriteAsPropertyName(Utf8JsonWriter writer,ResourceType value,JsonSerializerOptions options)=>writer.WritePropertyName(ResourceIds.Name(value));
}

// TOML tables have text keys; parse them once into enum-keyed recipe dictionaries.
public sealed class ResourceTableConverter:Tomlyn.Serialization.TomlConverter<Dictionary<ResourceType,float>>
{
    public override Dictionary<ResourceType,float> Read(Tomlyn.Serialization.TomlReader reader)
    {
        if(reader.TokenType!=Tomlyn.Serialization.TomlTokenType.StartTable)throw new InvalidDataException("Expected resource table.");
        var result=new Dictionary<ResourceType,float>();
        while(reader.Read()&&reader.TokenType!=Tomlyn.Serialization.TomlTokenType.EndTable)
        {
            if(reader.TokenType!=Tomlyn.Serialization.TomlTokenType.PropertyName)throw new InvalidDataException("Expected resource name.");
            var id=ResourceIds.Parse(reader.PropertyName!);
            if(!reader.Read())throw new InvalidDataException("Missing resource amount.");
            float amount=reader.TokenType switch {Tomlyn.Serialization.TomlTokenType.Integer=>(float)reader.GetInt64(),Tomlyn.Serialization.TomlTokenType.Float=>(float)reader.GetDouble(),_=>throw new InvalidDataException("Expected numeric resource amount.")};
            if(!float.IsFinite(amount)||!result.TryAdd(id,amount))throw new InvalidDataException("Invalid or duplicate resource amount.");
        }
        reader.Read();return result;
    }
    public override void Write(Tomlyn.Serialization.TomlWriter writer,Dictionary<ResourceType,float> value)
    {
        writer.WriteStartInlineTable();
        foreach(var entry in value){writer.WritePropertyName(ResourceIds.Name(entry.Key));writer.WriteFloatValue(entry.Value);}
        writer.WriteEndInlineTable();
    }
}

public sealed class ResourceTomlConverter:Tomlyn.Serialization.TomlConverter<ResourceType>
{
    public override ResourceType Read(Tomlyn.Serialization.TomlReader reader){var resource=ResourceIds.Parse(reader.GetString()??string.Empty);reader.Read();return resource;}
    public override void Write(Tomlyn.Serialization.TomlWriter writer,ResourceType value)=>writer.WriteStringValue(ResourceIds.Name(value));
}


