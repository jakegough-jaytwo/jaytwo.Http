#if NET6_0_OR_GREATER

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace jaytwo.Http.Handlers.Polly;

public class PollyHttpClientMiddleware : IHttpClientMiddleware
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public PollyHttpClientMiddleware(Action<ResiliencePipelineBuilder<HttpResponseMessage>> config)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder<HttpResponseMessage>();
        config.Invoke(pipelineBuilder);
        _pipeline = pipelineBuilder.Build();
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        var resilienceContext = ResilienceContextPool.Shared.Get(cancellationToken);
        try
        {
            // Run your pipeline under Polly
            return await _pipeline.ExecuteAsync(
                static async (ResilienceContext rc, State s) => await s.Next(s.Request, rc.CancellationToken),
                resilienceContext,
                new State(request, next)).ConfigureAwait(false);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(resilienceContext);
        }
    }

    private readonly struct State
    {
        public readonly HttpRequestMessage Request;
        public readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Next;

        public State(
            HttpRequestMessage request,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Next = next ?? throw new ArgumentNullException(nameof(next));
        }
    }
}

#endif
