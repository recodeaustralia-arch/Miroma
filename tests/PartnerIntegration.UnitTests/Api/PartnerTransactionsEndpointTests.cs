using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Api.Security;

namespace PartnerIntegration.UnitTests.Api;

public sealed class PartnerTransactionsEndpointTests : IClassFixture<PartnerTransactionsEndpointTests.ApiFactory>
{
    private readonly HttpClient _client;

    public PartnerTransactionsEndpointTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(ApiKeyOptions.HeaderName, "test-api-key");
    }

    [Fact]
    public async Task Post_returns_400_when_amount_is_not_positive()
    {
        var payload = Sample(amount: 0);

        var response = await _client.PostAsJsonAsync("/api/v1/partner/transactions", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_returns_401_without_api_key()
    {
        _client.DefaultRequestHeaders.Remove(ApiKeyOptions.HeaderName);

        var response = await _client.PostAsJsonAsync("/api/v1/partner/transactions", Sample());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_returns_202_when_partner_is_verified()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/partner/transactions", Sample());

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PartnerTransactionAcceptedResponse>();
        Assert.Equal("TXN-99823", body?.TransactionReference);
    }

    [Fact]
    public async Task Mock_verification_endpoint_returns_504_on_timeout()
    {
        var factory = new ApiFactory();
        var client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IRandomProvider>();
                services.AddSingleton<IRandomProvider>(new FixedRandom(0.0));
            });
        }).CreateClient();

        var response = await client.GetAsync("/api/v1/mock/partners/P-1001/verify");

        Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
    }

    private static object Sample(decimal amount = 250.00m) => new
    {
        partnerId = "P-1001",
        transactionReference = "TXN-99823",
        amount,
        currency = "USD",
        timestamp = "2024-05-10T14:30:00Z"
    };

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ApiKey:Keys:0", "test-api-key");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPartnerVerificationClient>();
                services.RemoveAll<ITransactionQueuePublisher>();
                services.AddSingleton<IPartnerVerificationClient, StubVerificationClient>();
                services.AddSingleton<ITransactionQueuePublisher, StubPublisher>();
            });
        }
    }

    private sealed class StubVerificationClient : IPartnerVerificationClient
    {
        public Task<PartnerVerificationResponse> VerifyAsync(string partnerId, CancellationToken cancellationToken) =>
            Task.FromResult(new PartnerVerificationResponse(partnerId, partnerId == "P-1001", "Acme"));
    }

    private sealed class StubPublisher : ITransactionQueuePublisher
    {
        public Task PublishAsync(PartnerTransactionMessage message, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FixedRandom : IRandomProvider
    {
        private readonly double _value;
        public FixedRandom(double value) => _value = value;
        public double NextDouble() => _value;
    }
}
