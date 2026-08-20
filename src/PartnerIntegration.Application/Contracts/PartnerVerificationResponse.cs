namespace PartnerIntegration.Application.Contracts;

public sealed record PartnerVerificationResponse(
    string PartnerId,
    bool IsVerified,
    string? PartnerName);
