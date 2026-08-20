using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Application.Abstractions;

public interface IPartnerVerificationClient
{
    Task<PartnerVerificationResponse> VerifyAsync(string partnerId, CancellationToken cancellationToken);
}
