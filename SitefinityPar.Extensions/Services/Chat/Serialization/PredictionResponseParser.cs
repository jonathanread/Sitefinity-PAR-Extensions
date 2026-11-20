using SitefinityWebApp.Services.Chat.Models;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal static class PredictionResponseParser
    {
        public static string Parse(string body)
        {
            var answer = new StringBuilder();
            string configurationName = null;
            using (var reader = new StringReader(body))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var response = JsonSerializer.Deserialize<PredictionChunk>(line, ChatJson.ResponseOptions);
                    var chunk = response?.Chunk;
                    if (chunk?.Type == null)
                        throw new InvalidDataException("The prediction response contains an invalid chunk.");
                    if (chunk.Type == "error")
                        throw new InvalidDataException("The prediction stream reported an error.");
                    if (chunk.Type == "object")
                    {
                        if (chunk.Object == null)
                            throw new InvalidDataException("The prediction response contains an invalid object.");
                        var category = ParseCategory(chunk.Object);
                        if (configurationName != null && !string.Equals(configurationName, category, StringComparison.Ordinal))
                            throw new InvalidDataException("Prediction returned conflicting categories.");
                        configurationName = category;
                    }
                    else if (chunk.Type == "text")
                    {
                        if (chunk.Text == null)
                            throw new InvalidDataException("The prediction response contains invalid text.");
                        answer.Append(chunk.Text);
                    }
                }
            }

            if (configurationName != null) return configurationName;
            var result = JsonSerializer.Deserialize<PredictionChunk.PredictionCategory>(answer.ToString(), ChatJson.ResponseOptions);
            return ParseCategory(result);
        }

        private static string ParseCategory(PredictionChunk.PredictionCategory result)
        {
            var category = result?.Category;
            if (string.IsNullOrWhiteSpace(category) || category.Length > 256)
                throw new InvalidDataException("Prediction must return a valid category.");
            return category;
        }
    }
}
