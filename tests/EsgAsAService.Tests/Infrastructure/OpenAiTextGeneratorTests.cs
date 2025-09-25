using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EsgAsAService.Infrastructure.AI;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EsgAsAService.Tests.Infrastructure;

public class OpenAiTextGeneratorTests
{
    [Fact]
    public async Task MissingApiKeyReturnsDisabledMessage()
    {
        var http = new HttpClient(new HttpClientHandler());
        var opt = Options.Create(new OpenAiOptions { ApiKey = null, BaseUrl = "https://api.openai.com/v1", Model = "gpt-5" });
        var gen = new OpenAiTextGenerator(http, opt, NullLogger<OpenAiTextGenerator>.Instance);

        var text = await gen.GenerateAsync(new EsgAsAService.Application.Abstractions.NarrativeRequest(
            EsgAsAService.Application.Abstractions.NarrativeSection.Overview,
            "Acme", 2025, "Summary"));

        text.Should().Contain("AI disabled");
    }

    [Fact]
    public async Task HttpFailureReturnsPlaceholder()
    {
        var handler = new FailingHandler();
        var http = new HttpClient(handler);
        var opt = Options.Create(new OpenAiOptions { ApiKey = "sk-test", BaseUrl = "https://api.openai.com/v1", Model = "gpt-5" });
        var gen = new OpenAiTextGenerator(http, opt, NullLogger<OpenAiTextGenerator>.Instance);

        var text = await gen.GenerateAsync(new EsgAsAService.Application.Abstractions.NarrativeRequest(
            EsgAsAService.Application.Abstractions.NarrativeSection.Overview,
            "Acme", 2025, "Summary"));

        text.Should().Contain("Unable to generate narrative");
    }

    [Fact]
    public async Task SuccessParsesResponseAndSendsBearerAuth()
    {
        var handler = new CapturingHandler();
        var http = new HttpClient(handler);
        var opt = Options.Create(new OpenAiOptions { ApiKey = "sk-secret", BaseUrl = "https://api.openai.com/v1", Model = "gpt-5" });
        var gen = new OpenAiTextGenerator(http, opt, NullLogger<OpenAiTextGenerator>.Instance);

        handler.Next = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\n  \"choices\": [ { \"message\": { \"role\": \"assistant\", \"content\": \"hello world\" } } ]\n}")
        };

        var text = await gen.GenerateAsync(new EsgAsAService.Application.Abstractions.NarrativeRequest(
            EsgAsAService.Application.Abstractions.NarrativeSection.Overview,
            "Acme", 2025, "Summary"));

        text.Should().Be("hello world");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.AbsolutePath.Should().EndWith("/chat/completions");
        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("sk-secret");
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return Task.FromResult(resp);
        }
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public HttpResponseMessage? Next { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(Next ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
