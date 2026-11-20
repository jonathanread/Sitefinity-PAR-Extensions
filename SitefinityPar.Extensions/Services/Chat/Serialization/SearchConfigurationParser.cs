using SitefinityWebApp.Services.Chat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SitefinityWebApp.Services.Chat.Serialization
{
    internal static class SearchConfigurationParser
    {
        public static SearchConfiguration Parse(string responseBody, string requestedConfigurationName)
        {
            var knowledgeBox = JsonSerializer.Deserialize<KnowledgeBoxDetails>(responseBody, ChatJson.ResponseOptions);
            var configurations = knowledgeBox?.SearchConfigs?.Configurations;
            if (configurations == null)
                throw new InvalidDataException("Knowledge box details do not contain search configurations.");

            KnowledgeBoxDetails.Configuration selected = null;
            var availableNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in configurations)
            {
                if (item?.Id == null) continue;
                availableNames.Add(item.Id);
                if (string.Equals(item.Id, requestedConfigurationName, StringComparison.Ordinal))
                    selected = item;
            }

            if (selected == null)
                throw new KeyNotFoundException("The requested search configuration was not found.");
            if (selected.Routing?.UseRouting == null)
                throw new InvalidDataException("The search configuration has invalid routing settings.");

            var result = new SearchConfiguration { UseRouting = selected.Routing.UseRouting.Value };
            if (!result.UseRouting) return result;

            if (selected.GenerativeAnswer?.UseSystemPrompt == true)
            {
                if (selected.GenerativeAnswer.SystemPrompt == null)
                    throw new InvalidDataException("The search configuration has an invalid system prompt.");
                result.SystemPrompt = selected.GenerativeAnswer.SystemPrompt;
            }

            var details = selected.Routing.Details;
            if (details?.Rules == null)
                throw new InvalidDataException("The search configuration does not contain routing rules.");

            foreach (var rule in details.Rules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.Prompt) ||
                    rule.SearchConfigurationName == null || !availableNames.Contains(rule.SearchConfigurationName))
                    throw new InvalidDataException("A routing rule has an invalid prompt or destination.");

                result.Rules.Add(new RoutingRule
                {
                    Prompt = rule.Prompt,
                    SearchConfigurationName = rule.SearchConfigurationName
                });
            }

            if (result.Rules.Count == 0)
                throw new InvalidDataException("Routing is enabled but no routing rules are configured.");

            result.GenerativeModel = details.GenerativeModel;
            return result;
        }
    }
}
