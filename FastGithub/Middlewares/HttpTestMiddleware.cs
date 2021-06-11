﻿using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.Middlewares
{
    sealed class HttpTestMiddleware : IGithubMiddleware
    {
        private readonly IOptionsMonitor<GithubOptions> options;
        private readonly ILogger<HttpTestMiddleware> logger;

        public HttpTestMiddleware(
            IOptionsMonitor<GithubOptions> options,
            ILogger<HttpTestMiddleware> logger)
        {
            this.options = options;
            this.logger = logger;
        }

        public async Task InvokeAsync(GithubContext context, Func<Task> next)
        {
            try
            {
                var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://{context.Address}/manifest.json"),
                };
                request.Headers.Host = "github.com";

                using var httpClient = new HttpClient(new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                });

                var startTime = DateTime.Now;
                using var cancellationTokenSource = new CancellationTokenSource(this.options.CurrentValue.HttpTestTimeout);
                var response = await httpClient.SendAsync(request, cancellationTokenSource.Token);
                var media = response.EnsureSuccessStatusCode().Content.Headers.ContentType?.MediaType;

                if (string.Equals(media, "application/manifest+json", StringComparison.OrdinalIgnoreCase))
                {
                    context.HttpElapsed = DateTime.Now.Subtract(startTime);
                    await next();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogInformation($"{context.Address} {ex.Message}");
            }
        }
    }
}