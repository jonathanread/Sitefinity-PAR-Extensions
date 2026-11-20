using System.Collections.Generic;

namespace SitefinityWebApp.Services.Chat.Models
{
    internal sealed class SearchConfiguration
    {
        public bool UseRouting { get; set; }
        public string GenerativeModel { get; set; }
        public string SystemPrompt { get; set; }
        public List<RoutingRule> Rules { get; } = new List<RoutingRule>();
    }
}
