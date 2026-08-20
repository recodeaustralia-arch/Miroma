namespace PartnerIntegration.Application.Contracts;

public enum ProcessTransactionStatus
{
    Accepted,
    PartnerNotVerified,
    VerificationUnavailable,
    PublishFailed
}

public sealed record ProcessTransactionResult(
    ProcessTransactionStatus Status,
    string Message,
    string? TransactionReference = null)
{
    public static ProcessTransactionResult Accepted(string transactionReference) =>
        new(ProcessTransactionStatus.Accepted, "Transaction accepted and queued for processing.", transactionReference);

    public static ProcessTransactionResult PartnerNotVerified(string partnerId) =>
        new(ProcessTransactionStatus.PartnerNotVerified, $"Partner '{partnerId}' is not verified.");

    public static ProcessTransactionResult VerificationUnavailable() =>
        new(ProcessTransactionStatus.VerificationUnavailable, "Partner verification is temporarily unavailable. Please retry.");

    public static ProcessTransactionResult PublishFailed() =>
        new(ProcessTransactionStatus.PublishFailed, "Transaction could not be queued. Please retry.");
}
