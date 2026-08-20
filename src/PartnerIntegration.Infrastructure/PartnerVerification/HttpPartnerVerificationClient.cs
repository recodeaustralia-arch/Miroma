using System.Net;
using System.Net.Http.Json;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class HttpPartnerVerificationClient : IPartnerVerificationClient
{
    private readonly HttpClient _httpClient;

    public HttpPartnerVerificationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PartnerVerificationResponse> VerifyAsync(string partnerId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"api/v1/mock/partners/{Uri.EscapeDataString(partnerId)}/verify",
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.GatewayTimeout
            or HttpStatusCode.RequestTimeout
            or >= HttpStatusCode.InternalServerError)
        {
            throw new TimeoutException(
                $"Partner verification API returned {(int)response.StatusCode} for partner '{partnerId}'.");
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<PartnerVerificationResponse>(cancellationToken);
        return payload ?? throw new InvalidOperationException("Partner verification API returned an empty body.");
    }
}
