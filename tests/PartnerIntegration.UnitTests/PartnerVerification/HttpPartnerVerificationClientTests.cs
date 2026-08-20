using System.Net;
using System.Net.Http.Json;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Infrastructure.PartnerVerification;

namespace PartnerIntegration.UnitTests.PartnerVerification;

public sealed class HttpPartnerVerificationClientTests
{
    [Fact]
    public async Task Maps_success_payload()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new PartnerVerificationResponse("P-1001", true, "Acme"))
        });

        var client = new HttpPartnerVerificationClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://verification.test/")
        });

        var result = await client.VerifyAsync("P-1001", CancellationToken.None);

        Assert.True(result.IsVerified);
        Assert.Equal("P-1001", handler.LastRequest?.RequestUri?.OriginalString.Split('/').TakeLast(2).First());
    }

    [Theory]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Converts_timeout_and_server_errors_to_TimeoutException(HttpStatusCode statusCode)
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(statusCode));
        var client = new HttpPartnerVerificationClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("http://verification.test/")
        });

        await Assert.ThrowsAsync<TimeoutException>(() => client.VerifyAsync("P-1001", CancellationToken.None));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
