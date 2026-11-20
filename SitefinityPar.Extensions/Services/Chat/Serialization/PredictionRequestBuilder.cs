using Progress.Sitefinity.AgenticRAGConnector.Clients.Models.Requests;
using SitefinityWebApp.Services.Chat.Models;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal static class PredictionRequestBuilder
    {
        public static string Build(AskRequest request, SearchConfiguration configuration)
        {
            var categoryDescription = new StringBuilder("Select the ID of the category that best matches the question:");
            foreach (var rule in configuration.Rules)
            {
                categoryDescription.AppendLine().Append("- ").Append(rule.SearchConfigurationName)
                    .Append(": ").Append(rule.Prompt);
            }

            var payload = new PredictionRequest
            {
                Question = request.Query,
                UserId = "USER",
                GenerativeModel = string.IsNullOrWhiteSpace(configuration.GenerativeModel) ? null : configuration.GenerativeModel,
                UserPrompt = new Prompt { Value = "{question}" },
                System = configuration.SystemPrompt,
                Retrieval = false,
                ChatHistory = request.ChatHistory ?? new List<AgenticRAGContextItemDto>(),
                JsonSchema = new Schema
                {
                    Name = "routing",
                    Description = "User intent analysis. The objective is to return the id of the most appropriate category for the given question",
                    Parameters = new SchemaParameters
                    {
                        Type = "object",
                        Properties = new SchemaProperties
                        {
                            Category = new SchemaCategory { Type = "string", Description = categoryDescription.ToString() }
                        },
                        Required = new[] { "category" }
                    }
                }
            };

            return JsonSerializer.Serialize(payload);
        }

        private sealed class PredictionRequest
        {
            [JsonPropertyName("question")]
            public string Question { get; set; }

            [JsonPropertyName("user_id")]
            public string UserId { get; set; }

            [JsonPropertyName("generative_model")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string GenerativeModel { get; set; }

            [JsonPropertyName("user_prompt")]
            public Prompt UserPrompt { get; set; }

            [JsonPropertyName("system")]
            public string System { get; set; }

            [JsonPropertyName("retrieval")]
            public bool Retrieval { get; set; }

            [JsonPropertyName("chat_history")]
            public List<AgenticRAGContextItemDto> ChatHistory { get; set; }

            [JsonPropertyName("json_schema")]
            public Schema JsonSchema { get; set; }
        }

        private sealed class Prompt
        {
            [JsonPropertyName("prompt")]
            public string Value { get; set; }
        }

        private sealed class Schema
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }

            [JsonPropertyName("parameters")]
            public SchemaParameters Parameters { get; set; }
        }

        private sealed class SchemaParameters
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("properties")]
            public SchemaProperties Properties { get; set; }

            [JsonPropertyName("required")]
            public string[] Required { get; set; }
        }

        private sealed class SchemaProperties
        {
            [JsonPropertyName("category")]
            public SchemaCategory Category { get; set; }
        }

        private sealed class SchemaCategory
        {
            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("description")]
            public string Description { get; set; }
        }
    }
}
