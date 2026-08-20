using PartnerIntegration.Application.Abstractions;

namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class SystemRandomProvider : IRandomProvider
{
    public double NextDouble() => Random.Shared.NextDouble();
}
