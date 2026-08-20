namespace PartnerIntegration.Application.Contracts;

public sealed record PartnerTransactionMessage(
    string PartnerId,
    string TransactionReference,
    decimal Amount,
    string Currency,
    DateTimeOffset Timestamp,
    DateTimeOffset EnqueuedAtUtc)
{
    public static PartnerTransactionMessage From(PartnerTransactionRequest request) =>
        new(
            request.PartnerId!,
            request.TransactionReference!,
            request.Amount!.Value,
            request.Currency!.ToUpperInvariant(),
            request.Timestamp!.Value,
            DateTimeOffset.UtcNow);
}
