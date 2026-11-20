using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal sealed class OptionalBooleanConverter : JsonConverter<bool?>
    {
        public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.True) return true;
            if (reader.TokenType == JsonTokenType.False) return false;

            using (JsonDocument.ParseValue(ref reader)) { }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
        {
            if (value.HasValue) writer.WriteBooleanValue(value.Value);
            else writer.WriteNullValue();
        }
    }
}
