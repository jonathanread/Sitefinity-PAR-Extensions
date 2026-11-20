using Progress.Sitefinity.AgenticRAGConnector.Clients.Models.Requests;
using Progress.Sitefinity.AgenticRAGConnector.Configuration;
using Progress.Sitefinity.AgenticRAGConnector.Controllers;
using SitefinityWebApp.Services.Chat;
using SitefinityWebApp.Services.Chat.Clients;
using SitefinityWebApp.Services.Chat.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Telerik.Sitefinity.Abstractions;
using Telerik.Sitefinity.Configuration;

namespace SitefinityWebApp.Services.Controllers
{
    public class CustomChatController : AgenticRagController
    {
        private static readonly SearchConfigurationService SearchConfigurations =
            new SearchConfigurationService(AgenticRagHttpClient.Shared);
        private static readonly PredictionService Predictions =
            new PredictionService(AgenticRagHttpClient.Shared);

        /// <summary>
        /// Uses predict/chat to select a search configuration, then delegates answering to Sitefinity.
        /// </summary>
        /// <remarks>
        /// Loads the requested search configuration from the account knowledge-box details,
        /// caching successful responses per knowledge box and configuration for the configured duration
        /// (five minutes by default). If its routing is enabled, prediction selects one of its configured destinations;
        /// otherwise the requested configuration is sent directly to base.Chat.
        /// The POST body remains the standard AskRequest; cancellation is handled server-side.
        /// Outbound requests share a bounded connection pool.
        /// </remarks>
        [HttpPost]
        public async Task<HttpResponseMessage> CustomChat(AskRequest request)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Query) ||
                string.IsNullOrWhiteSpace(request.KnowledgeBoxName) ||
                string.IsNullOrWhiteSpace(request.ConfigurationName))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "A query, knowledgeBoxName and search configuration name are required.");
            }

            var agenticRagConfig = Config.Get<AgenticRAGConfig>();
            KnowledgeBoxSettings knowledgeBox;
            if (!agenticRagConfig.KnowledgeBoxes.TryGetValue(request.KnowledgeBoxName, out knowledgeBox))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "The requested knowledge box is not configured in Sitefinity.");
            }

            var accountId = ConfigurationManager.AppSettings["AgenticRAGAccountId"];
            var baseUrl = knowledgeBox.BaseUrl;
            Uri knowledgeBoxDetailsEndpoint;
            Uri predictEndpoint;
            if (!Guid.TryParse(accountId, out _) ||
                !TryBuildEndpoint(baseUrl, knowledgeBox.KnowledgeBoxId, out predictEndpoint) ||
                !TryBuildAccountKnowledgeBoxEndpoint(baseUrl, accountId, knowledgeBox.KnowledgeBoxId,
                    out knowledgeBoxDetailsEndpoint) ||
                string.IsNullOrWhiteSpace(knowledgeBox.KnowledgeBoxToken))
            {
                Log.Write("Custom chat requires a valid Agentic RAG base URL, account ID, knowledge box ID and token.",
                    ConfigurationPolicy.ErrorLog);
                return Request.CreateErrorResponse(HttpStatusCode.ServiceUnavailable,
                    "The Agentic RAG connection is not configured correctly.");
            }

            var disconnectToken = HttpContext.Current == null
                ? CancellationToken.None
                : HttpContext.Current.Response.ClientDisconnectedToken;

            SearchConfiguration routingConfiguration;
            string configurationName;
            try
            {
                routingConfiguration = await SearchConfigurations.GetAsync(knowledgeBoxDetailsEndpoint,
                    knowledgeBox.KnowledgeBoxToken, request.ConfigurationName, disconnectToken);
                configurationName = routingConfiguration.UseRouting
                    ? await Predictions.SelectConfigurationAsync(predictEndpoint, knowledgeBox.KnowledgeBoxToken,
                        request, routingConfiguration, disconnectToken)
                    : request.ConfigurationName;
            }
            catch (OperationCanceledException) when (!disconnectToken.IsCancellationRequested)
            {
                Log.Write("The Agentic RAG configuration or prediction request timed out.", ConfigurationPolicy.ErrorLog);
                return Request.CreateErrorResponse(HttpStatusCode.GatewayTimeout,
                    "The Agentic RAG request timed out.");
            }
            catch (KeyNotFoundException exception)
            {
                Log.Write(exception, ConfigurationPolicy.ErrorLog);
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest,
                    "The requested search configuration was not found in the knowledge box.");
            }
            catch (HttpRequestException exception)
            {
                Log.Write(exception, ConfigurationPolicy.ErrorLog);
                return Request.CreateErrorResponse(HttpStatusCode.BadGateway,
                    "The Agentic RAG configuration or prediction request failed.");
            }
            catch (InvalidDataException exception)
            {
                Log.Write(exception, ConfigurationPolicy.ErrorLog);
                return Request.CreateErrorResponse(HttpStatusCode.BadGateway,
                    "The Agentic RAG configuration or routing response is invalid.");
            }
            catch (JsonException exception)
            {
                Log.Write(exception, ConfigurationPolicy.ErrorLog);
                return Request.CreateErrorResponse(HttpStatusCode.BadGateway,
                    "The Agentic RAG configuration or prediction response is invalid.");
            }

            disconnectToken.ThrowIfCancellationRequested();
            request.ConfigurationName = configurationName;
            if (routingConfiguration.UseRouting)
            {
                request.RagStrategies = null;
                request.FilterExpression = null;
                request.Prompt = null;
            }

            return await base.Chat(request);
        }

        private static bool TryBuildAccountKnowledgeBoxEndpoint(string baseUrl, string accountId,
            string knowledgeBoxId, out Uri endpoint)
        {
            endpoint = null;
            Uri origin;
            if (string.IsNullOrWhiteSpace(knowledgeBoxId) ||
                !Guid.TryParse(accountId, out _) ||
                !Uri.TryCreate(baseUrl, UriKind.Absolute, out origin) ||
                origin.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(origin.UserInfo) ||
                origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) ||
                !string.IsNullOrEmpty(origin.Fragment))
            {
                return false;
            }

            endpoint = new Uri(origin, "api/v1/account/" + Uri.EscapeDataString(accountId) +
                "/kb/" + Uri.EscapeDataString(knowledgeBoxId));
            return true;
        }

        private static bool TryBuildEndpoint(string baseUrl, string knowledgeBoxId, out Uri endpoint)
        {
            endpoint = null;
            Uri origin;
            if (string.IsNullOrWhiteSpace(knowledgeBoxId) ||
                !Uri.TryCreate(baseUrl, UriKind.Absolute, out origin) ||
                origin.Scheme != Uri.UriSchemeHttps ||
                !string.IsNullOrEmpty(origin.UserInfo) ||
                origin.AbsolutePath != "/" || !string.IsNullOrEmpty(origin.Query) ||
                !string.IsNullOrEmpty(origin.Fragment))
            {
                return false;
            }

            endpoint = new Uri(origin, "api/v1/kb/" + Uri.EscapeDataString(knowledgeBoxId) + "/predict/chat");
            return true;
        }


    }
}
