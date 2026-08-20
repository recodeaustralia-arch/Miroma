namespace PartnerIntegration.Infrastructure.PartnerVerification;

public sealed class PartnerVerificationOptions
{
    public const string SectionName = "PartnerVerification";

    public string BaseUrl { get; set; } = "http://localhost:5080";
    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelayMilliseconds { get; set; } = 200;
    public int HttpTimeoutSeconds { get; set; } = 5;
}
