using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Exceptions;
using Polly;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class ResilientPartnerVerificationClient : IPartnerVerificationClient
{
    private readonly IPartnerVerificationClient _inner;
    private readonly ResiliencePipeline<PartnerVerificationResponse> _pipeline;
    private readonly ILogger<ResilientPartnerVerificationClient> _logger;

    public ResilientPartnerVerificationClient(
        HttpPartnerVerificationClient inner,
        IOptions<PartnerVerificationOptions> options,
        ILogger<ResilientPartnerVerificationClient> logger)
        : this(inner, PartnerVerificationResilience.CreatePipeline(options.Value), logger)
    {
    }

    public ResilientPartnerVerificationClient(
        IPartnerVerificationClient inner,
        ResiliencePipeline<PartnerVerificationResponse> pipeline,
        ILogger<ResilientPartnerVerificationClient> logger)
    {
        _inner = inner;
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task<PartnerVerificationResponse> VerifyAsync(string partnerId, CancellationToken cancellationToken)
    {
        var attempt = 0;

        try
        {
            return await _pipeline.ExecuteAsync(async token =>
            {
                attempt++;
                if (attempt > 1)
                {
                    _logger.LogWarning(
                        "Retrying partner verification for {PartnerId} (attempt {Attempt})",
                        partnerId,
                        attempt);
                }

                return await _inner.VerifyAsync(partnerId, token);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is TimeoutException or HttpRequestException or TaskCanceledException)
        {
            throw new PartnerVerificationUnavailableException(
                $"Partner verification failed after {attempt} attempt(s) for partner '{partnerId}'.",
                ex);
        }
    }
}
