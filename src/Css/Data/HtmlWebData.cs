using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Css.Data;

public record class HtmlElementData(string Name, DescriptionData Description, IReadOnlyList<HtmlAttributeData> Attributes);
public record class HtmlAttributeData(string Name, DescriptionData Description, string? ValueSet);
public record class HtmlAttributeValueSet(string Name, IReadOnlyList<HtmlAttributeValueData> Values);
public record class HtmlAttributeValueData(string Name);

public record struct DescriptionData(string Value);

public record class HtmlWebData(
    IReadOnlyList<HtmlElementData> Tags,
    IReadOnlyList<HtmlAttributeData> GlobalAttributes,
    IReadOnlyList<HtmlAttributeValueSet> ValueSets
)
{
    public static HtmlWebData Load()
    {
        var type = typeof(HtmlWebData).Assembly;
        using var stream = type.GetManifestResourceStream($"{type.GetName().Name}.Data.html.json");
        using var reader = new JsonTextReader(new StreamReader(stream));
        var serializer = new JsonSerializer();
        serializer.Converters.Add(new DescriptionConverter());
        return serializer.Deserialize<HtmlWebData>(reader);
    }

    public static HtmlWebDataIndex Index = new(Load());
}

public record class HtmlWebDataIndex(HtmlWebData Data)
{

    public IReadOnlyDictionary<string, HtmlElementData> Elements { get; } = Data.Tags.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<HtmlElementData> ElementsSorted { get; } = Data.Tags.OrderBy(p => p.Name).ToList();


    public IReadOnlyDictionary<string, HtmlAttributeData> GlobalAttributes { get; } = Data.GlobalAttributes.ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<HtmlAttributeData> GlobalAttributesSorted { get; } = Data.GlobalAttributes.OrderBy(p => p.Name).ToList();

    public IReadOnlyDictionary<string, HtmlAttributeValueSet> ValueSets { get; } = Data.ValueSets.ToDictionary(p => p.Name, p => p);

}


internal class DescriptionConverter : JsonConverter<DescriptionData>
{
    private static readonly JsonSerializer _inner = new();

    public override DescriptionData ReadJson(JsonReader reader, Type objectType, DescriptionData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        /*JObject obj = JObject.Load(reader);
        if (obj is JToken { Type: JTokenType.String })
        {
            return new(obj.Value<string>());
        }
        
        return obj.ToObject<DescriptionData>()!;*/

        JToken token = JToken.Load(reader);

        if (token.Type == JTokenType.Null)
        {
            return default;
        }
        else if (token.Type == JTokenType.String)
        {
            // Directly parse the string
            return new DescriptionData(token.ToString());
        }
        else if (token.Type == JTokenType.Object)
        {
            // Extract the 'Value' property from the object
            return new DescriptionData(token["value"].ToString());
        }
        else
        {
            return default;
        }

        switch (reader.TokenType)
        {
            case JsonToken.Null:
                reader.Read();
                return default;
                 
            case JsonToken.String:
                string value = reader.ReadAsString();
                //reader.Read();
                return new(value);

            case JsonToken.StartObject:
                var obj = JObject.Load(reader);
                if (reader.TokenType == JsonToken.EndObject)
                {
                    reader.Read();
                }
                return obj.ToObject<DescriptionData>();
                var inner = new JsonSerializer();
                return inner.Deserialize<DescriptionData>(reader);

            default:
                return default;
        }
    }

    public override void WriteJson(JsonWriter writer, DescriptionData value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
