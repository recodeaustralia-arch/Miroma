namespace PartnerIntegration.Api.Security;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string SchemeName = "ApiKey";

    public string[] Keys { get; set; } = [];
}
