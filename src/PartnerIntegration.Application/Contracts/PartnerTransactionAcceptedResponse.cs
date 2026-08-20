namespace PartnerIntegration.Application.Contracts;

public sealed record PartnerTransactionAcceptedResponse(
    string TransactionReference,
    string Status,
    string Message);
