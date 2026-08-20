using PartnerIntegration.Application.Abstractions;
using PartnerIntegration.Infrastructure.PartnerVerification;

namespace PartnerIntegration.UnitTests.PartnerVerification;

public sealed class FlakyPartnerVerificationServiceTests
{
    [Fact]
    public void Throws_TimeoutException_when_random_draw_is_in_failure_band()
    {
        var service = new FlakyPartnerVerificationService(new FixedRandom(0.29));

        var act = () => service.Verify("P-1001");

        var exception = Assert.Throws<TimeoutException>(act);
        Assert.Contains("P-1001", exception.Message);
    }

    [Fact]
    public void Returns_verified_for_known_partner_when_random_draw_succeeds()
    {
        var service = new FlakyPartnerVerificationService(new FixedRandom(0.30));

        var result = service.Verify("P-1001");

        Assert.True(result.IsVerified);
        Assert.Equal("P-1001", result.PartnerId);
        Assert.Equal("Verified Partner", result.PartnerName);
    }

    [Fact]
    public void Returns_unverified_for_unknown_partner_when_call_succeeds()
    {
        var service = new FlakyPartnerVerificationService(new FixedRandom(0.99));

        var result = service.Verify("P-UNKNOWN");

        Assert.False(result.IsVerified);
        Assert.Null(result.PartnerName);
    }

    private sealed class FixedRandom : IRandomProvider
    {
        private readonly double _value;

        public FixedRandom(double value) => _value = value;

        public double NextDouble() => _value;
    }
}
