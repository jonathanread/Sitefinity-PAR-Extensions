using SitefinityWebApp.Services.Chat.Serialization;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SitefinityWebApp.Services.Chat.Models
{
    internal sealed class KnowledgeBoxDetails
    {
        [JsonPropertyName("search_configs")]
        [JsonConverter(typeof(OptionalObjectConverter<SearchConfigCollection>))]
        public SearchConfigCollection SearchConfigs { get; set; }

        internal sealed class SearchConfigCollection
        {
            [JsonPropertyName("searchConfigurations")]
            [JsonConverter(typeof(OptionalObjectListConverter<Configuration>))]
            public List<Configuration> Configurations { get; set; }
        }

        internal sealed class Configuration
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("routing")]
            [JsonConverter(typeof(OptionalObjectConverter<RoutingSettings>))]
            public RoutingSettings Routing { get; set; }

            [JsonPropertyName("generativeAnswer")]
            [JsonConverter(typeof(OptionalObjectConverter<GenerativeAnswerSettings>))]
            public GenerativeAnswerSettings GenerativeAnswer { get; set; }
        }

        internal sealed class RoutingSettings
        {
            [JsonPropertyName("useRouting")]
            public bool? UseRouting { get; set; }

            [JsonPropertyName("routing")]
            [JsonConverter(typeof(OptionalObjectConverter<RoutingDetails>))]
            public RoutingDetails Details { get; set; }
        }

        internal sealed class RoutingDetails
        {
            [JsonPropertyName("rules")]
            [JsonConverter(typeof(OptionalObjectListConverter<Rule>))]
            public List<Rule> Rules { get; set; }

            [JsonPropertyName("generative_model")]
            public string GenerativeModel { get; set; }
        }

        internal sealed class Rule
        {
            [JsonPropertyName("prompt")]
            public string Prompt { get; set; }

            [JsonPropertyName("search_config")]
            public string SearchConfigurationName { get; set; }
        }

        internal sealed class GenerativeAnswerSettings
        {
            [JsonPropertyName("useSystemPrompt")]
            public bool? UseSystemPrompt { get; set; }

            [JsonPropertyName("systemPrompt")]
            public string SystemPrompt { get; set; }
        }
    }
}
