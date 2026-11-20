using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal sealed class OptionalStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString();

            using (JsonDocument.ParseValue(ref reader)) { }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
