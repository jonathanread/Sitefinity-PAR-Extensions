using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal sealed class OptionalObjectConverter<T> : JsonConverter<T> where T : class
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using (var document = JsonDocument.ParseValue(ref reader))
            {
                return document.RootElement.ValueKind == JsonValueKind.Object
                    ? JsonSerializer.Deserialize<T>(document.RootElement.GetRawText(), options)
                    : null;
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}
