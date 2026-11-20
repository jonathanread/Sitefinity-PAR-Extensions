using SitefinityWebApp.Services.Chat.Serialization;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Models
{
    internal sealed class PredictionChunk
    {
        [JsonPropertyName("chunk")]
        [JsonConverter(typeof(OptionalObjectConverter<ChunkData>))]
        public ChunkData Chunk { get; set; }

        internal sealed class ChunkData
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("object")]
            [JsonConverter(typeof(OptionalObjectConverter<PredictionCategory>))]
            public PredictionCategory Object { get; set; }

            [JsonPropertyName("text")]
            public string Text { get; set; }
        }

        internal sealed class PredictionCategory
        {
            [JsonPropertyName("category")]
            public string Category { get; set; }
        }
    }
}
