using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SitefinityWebApp.Services.Chat.Clients
{
    internal sealed class AgenticRagHttpClient
    {
        private const int MaxConcurrentRequests = 16;
        private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(2);

        public static readonly AgenticRagHttpClient Shared = new AgenticRagHttpClient(
            new HttpClientHandler
            {
                MaxConnectionsPerServer = MaxConcurrentRequests,
                AllowAutoRedirect = false,
                UseCookies = false
            }, MaxConcurrentRequests, RequestTimeout);

        private readonly HttpClient client;
        private readonly SemaphoreSlim requestSlots;
        private readonly TimeSpan timeout;

        internal AgenticRagHttpClient(HttpMessageHandler handler, int maxConcurrentRequests, TimeSpan timeout)
        {
            this.client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            this.requestSlots = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
            this.timeout = timeout;
        }

        public async Task<string> SendAsync(HttpRequestMessage request, string token, string accept,
            long maxResponseBytes, CancellationToken cancellationToken)
        {
            request.Headers.Add("X-NUCLIA-SERVICEACCOUNT", "Bearer " + token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

            using (var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                lifetime.CancelAfter(this.timeout);
                await this.requestSlots.WaitAsync(lifetime.Token).ConfigureAwait(false);
                try
                {
                    using (var response = await this.client.SendAsync(request,
                        HttpCompletionOption.ResponseHeadersRead, lifetime.Token).ConfigureAwait(false))
                    using (lifetime.Token.Register(response.Dispose))
                    {
                        response.EnsureSuccessStatusCode();
                        await response.Content.LoadIntoBufferAsync(maxResponseBytes).ConfigureAwait(false);
                        lifetime.Token.ThrowIfCancellationRequested();
                        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        lifetime.Token.ThrowIfCancellationRequested();
                        return body;
                    }
                }
                catch (Exception exception) when (lifetime.IsCancellationRequested &&
                    (exception is HttpRequestException || exception is IOException ||
                     exception is ObjectDisposedException))
                {
                    throw new OperationCanceledException("The Agentic RAG request was cancelled.", exception, lifetime.Token);
                }
                finally
                {
                    this.requestSlots.Release();
                }
            }
        }
    }
}
