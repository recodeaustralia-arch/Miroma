using Microsoft.Extensions.Logging;
using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Exceptions;

namespace PartnerIntegration.Application.Services;

public sealed class PartnerTransactionProcessor
{
    private readonly IPartnerVerificationClient _partnerVerificationClient;
    private readonly ITransactionQueuePublisher _publisher;
    private readonly ILogger<PartnerTransactionProcessor> _logger;

    public PartnerTransactionProcessor(
        IPartnerVerificationClient partnerVerificationClient,
        ITransactionQueuePublisher publisher,
        ILogger<PartnerTransactionProcessor> logger)
    {
        _partnerVerificationClient = partnerVerificationClient;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ProcessTransactionResult> ProcessAsync(
        PartnerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        PartnerVerificationResponse verification;
        try
        {
            verification = await _partnerVerificationClient.VerifyAsync(request.PartnerId!, cancellationToken);
        }
        catch (PartnerVerificationUnavailableException ex)
        {
            _logger.LogWarning(ex, "Partner verification exhausted retries for {PartnerId}", request.PartnerId);
            return ProcessTransactionResult.VerificationUnavailable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected partner verification failure for {PartnerId}", request.PartnerId);
            return ProcessTransactionResult.VerificationUnavailable();
        }

        if (!verification.IsVerified)
        {
            return ProcessTransactionResult.PartnerNotVerified(request.PartnerId!);
        }

        var message = PartnerTransactionMessage.From(request);

        try
        {
            await _publisher.PublishAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue transaction {TransactionReference}", message.TransactionReference);
            return ProcessTransactionResult.PublishFailed();
        }

        _logger.LogInformation(
            "Queued transaction {TransactionReference} for partner {PartnerId}",
            message.TransactionReference,
            message.PartnerId);

        return ProcessTransactionResult.Accepted(message.TransactionReference);
    }
}
