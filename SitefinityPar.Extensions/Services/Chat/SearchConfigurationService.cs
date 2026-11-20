using SitefinityWebApp.Services.Chat.Clients;
using SitefinityWebApp.Services.Chat.Models;
using SitefinityWebApp.Services.Chat.Serialization;
using System;
using System.Configuration;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Caching;
using Telerik.Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;
using Telerik.Sitefinity.Services;

namespace SitefinityWebApp.Services.Chat
{
    internal sealed class SearchConfigurationService
    {
        private const int DefaultCacheDurationMinutes = 5;
        private const string CacheDurationSettingName = "AgenticRAGSearchConfigurationCacheDurationMinutes";
        private readonly AgenticRagHttpClient client;

        public SearchConfigurationService(AgenticRagHttpClient client)
        {
            this.client = client;
        }

        public async Task<SearchConfiguration> GetAsync(Uri endpoint, string token,
            string requestedConfigurationName, CancellationToken cancellationToken)
        {
            var cacheManager = SystemManager.GetCacheManager(CacheManagerInstance.Global);
            var cacheItemKey = GetCacheKey(endpoint, requestedConfigurationName);
            var cachedResponse = cacheManager.GetData(cacheItemKey) as string;
            if (cachedResponse != null)
            {
                return SearchConfigurationParser.Parse(cachedResponse, requestedConfigurationName);
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, endpoint))
            {
                var body = await this.client.SendAsync(request, token, "application/json",
                    4 * 1024 * 1024, cancellationToken).ConfigureAwait(false);
                var configuration = SearchConfigurationParser.Parse(body, requestedConfigurationName);
                cacheManager.Add(cacheItemKey, body, CacheItemPriority.Normal, null,
                    new AbsoluteTime(GetCacheDuration()));
                return configuration;
            }
        }

        private static TimeSpan GetCacheDuration()
        {
            var configuredDuration = ConfigurationManager.AppSettings[CacheDurationSettingName];
            if (string.IsNullOrWhiteSpace(configuredDuration))
            {
                return TimeSpan.FromMinutes(DefaultCacheDurationMinutes);
            }

            int durationMinutes;
            if (!int.TryParse(configuredDuration, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out durationMinutes) || durationMinutes <= 0)
            {
                throw new ConfigurationErrorsException(string.Format(CultureInfo.InvariantCulture,
                    "The {0} app setting must be a positive whole number of minutes.", CacheDurationSettingName));
            }

            return TimeSpan.FromMinutes(durationMinutes);
        }

        private static string GetCacheKey(Uri endpoint, string requestedConfigurationName)
        {
            var keySource = endpoint.AbsoluteUri + "\n" + requestedConfigurationName;
            using (var sha256 = SHA256.Create())
            {
                return "CustomChat.SearchConfiguration." +
                    BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(keySource))).Replace("-", string.Empty);
            }
        }
    }
}
