using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using SogigiMind.Options;
using SogigiMind.Services;

namespace SogigiMind.Services
{
    public class DefaultRemoteFetchService : IRemoteFetchService
    {
        private readonly HttpClient _httpClient;

        public DefaultRemoteFetchService(HttpClient httpClient)
        {
            this._httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            return this._httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead);
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{

    public static class RemoteFetchServiceExtensions
    {
        public static IServiceCollection AddRemoteFetchService(this IServiceCollection services)
        {
            services
                .AddHttpClient<IRemoteFetchService, DefaultRemoteFetchService>((serviceProvider, httpClient) =>
                {
                    var options = serviceProvider.GetService<IOptionsMonitor<ThumbnailOptions>>()?.CurrentValue;
                    if (options != null)
                    {
                        httpClient.Timeout = options.FetchTimeout > TimeSpan.Zero
                            ? options.FetchTimeout : Timeout.InfiniteTimeSpan;
                    }

                    var accept = httpClient.DefaultRequestHeaders.Accept;
                    accept.Clear();
                    accept.Add(new MediaTypeWithQualityHeaderValue("image/*"));
                    accept.Add(new MediaTypeWithQualityHeaderValue("video/*"));

                    var userAgent = httpClient.DefaultRequestHeaders.UserAgent;
                    userAgent.Clear();
                    userAgent.Add(new ProductInfoHeaderValue("SogigiMind", typeof(DefaultRemoteFetchService).Assembly.GetName().Version?.ToString() ?? "0.0.0.0"));
                })
                .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
                {
                    var handler = new HttpClientHandler() { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 };

                    var options = serviceProvider.GetService<IOptionsMonitor<ThumbnailOptions>>()?.CurrentValue;
                    if (!string.IsNullOrEmpty(options?.Proxy)) handler.Proxy = new WebProxy(options.Proxy);

                    return handler;
                });
            return services;
        }
    }
}
