using Progress.Sitefinity.AgenticRAGConnector.Clients.Models.Requests;
using SitefinityWebApp.Services.Chat.Clients;
using SitefinityWebApp.Services.Chat.Models;
using SitefinityWebApp.Services.Chat.Serialization;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SitefinityWebApp.Services.Chat
{
    internal sealed class PredictionService
    {
        private readonly AgenticRagHttpClient client;

        public PredictionService(AgenticRagHttpClient client)
        {
            this.client = client;
        }

        public async Task<string> SelectConfigurationAsync(Uri endpoint, string token, AskRequest request,
            SearchConfiguration configuration, CancellationToken cancellationToken)
        {
            using (var message = new HttpRequestMessage(HttpMethod.Post, endpoint))
            {
                message.Content = new StringContent(PredictionRequestBuilder.Build(request, configuration), Encoding.UTF8, "application/json");
                var body = await this.client.SendAsync(message, token, "application/x-ndjson",
                    65536, cancellationToken).ConfigureAwait(false);
                var selectedName = PredictionResponseParser.Parse(body);
                foreach (var rule in configuration.Rules)
                {
                    if (string.Equals(rule.SearchConfigurationName, selectedName, StringComparison.Ordinal))
                    {
                        return selectedName;
                    }
                }

                throw new InvalidDataException("Prediction returned a destination not allowed by the routing rules.");
            }
        }
    }
}
