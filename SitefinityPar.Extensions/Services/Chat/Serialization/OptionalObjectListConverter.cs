using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal sealed class OptionalObjectListConverter<T> : JsonConverter<List<T>> where T : class
    {
        public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var document = JsonDocument.ParseValue(ref reader))
            {
                if (document.RootElement.ValueKind != JsonValueKind.Array) return null;
                var items = new List<T>();
                foreach (var item in document.RootElement.EnumerateArray())
                {
                    items.Add(item.ValueKind == JsonValueKind.Object
                        ? JsonSerializer.Deserialize<T>(item.GetRawText(), options)
                        : null);
                }
                return items;
            }
        }

        public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
