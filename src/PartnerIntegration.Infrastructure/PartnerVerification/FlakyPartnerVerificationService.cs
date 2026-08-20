using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class FlakyPartnerVerificationService
{
    public const double TimeoutProbability = 0.30;

    private static readonly HashSet<string> VerifiedPartners = new(StringComparer.OrdinalIgnoreCase)
    {
        "P-1001",
        "P-1002",
        "P-2000"
    };

    private readonly IRandomProvider _random;

    public FlakyPartnerVerificationService(IRandomProvider random)
    {
        _random = random;
    }

    public PartnerVerificationResponse Verify(string partnerId)
    {
        if (_random.NextDouble() < TimeoutProbability)
        {
            throw new TimeoutException($"Partner verification API timed out for partner '{partnerId}'.");
        }

        var verified = VerifiedPartners.Contains(partnerId);
        return new PartnerVerificationResponse(
            partnerId,
            verified,
            verified ? "Verified Partner" : null);
    }
}
