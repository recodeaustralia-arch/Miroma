using FluentValidation.TestHelper;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Validation;

namespace PartnerIntegration.UnitTests.Validation;

public sealed class PartnerTransactionRequestValidatorTests
{
    private readonly PartnerTransactionRequestValidator _validator = new();

    [Fact]
    public void Valid_payload_passes()
    {
        var result = _validator.TestValidate(ValidRequest());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void PartnerId_is_required(string? partnerId)
    {
        var request = ValidRequest();
        request.PartnerId = partnerId;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.PartnerId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TransactionReference_is_required(string? transactionReference)
    {
        var request = ValidRequest();
        request.TransactionReference = transactionReference;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.TransactionReference);
    }

    [Fact]
    public void Amount_is_required()
    {
        var request = ValidRequest();
        request.Amount = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-250.00)]
    public void Amount_must_be_greater_than_zero(decimal amount)
    {
        var request = ValidRequest();
        request.Amount = amount;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Amount!.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Currency_is_required(string? currency)
    {
        var request = ValidRequest();
        request.Currency = currency;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("XXX-INVALID")]
    [InlineData("usd1")]
    public void Currency_must_be_iso_4217(string currency)
    {
        var request = ValidRequest();
        request.Currency = currency;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Currency);
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("usd")]
    [InlineData("EUR")]
    [InlineData("AUD")]
    [InlineData("JPY")]
    public void Common_currencies_are_accepted(string currency)
    {
        var request = ValidRequest();
        request.Currency = currency;

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void Timestamp_is_required()
    {
        var request = ValidRequest();
        request.Timestamp = null;

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Timestamp);
    }

    private static PartnerTransactionRequest ValidRequest() => new()
    {
        PartnerId = "P-1001",
        TransactionReference = "TXN-99823",
        Amount = 250.00m,
        Currency = "USD",
        Timestamp = DateTimeOffset.Parse("2024-05-10T14:30:00Z")
    };
}
