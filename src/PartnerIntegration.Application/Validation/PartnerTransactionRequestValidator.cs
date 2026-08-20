using FluentValidation;
using PartnerIntegration.Application.Contracts;

namespace PartnerIntegration.Application.Validation;

public sealed class PartnerTransactionRequestValidator : AbstractValidator<PartnerTransactionRequest>
{
    public PartnerTransactionRequestValidator()
    {
        RuleFor(x => x.PartnerId)
            .NotEmpty()
            .WithMessage("partnerId is required.");

        RuleFor(x => x.TransactionReference)
            .NotEmpty()
            .WithMessage("transactionReference is required.");

        RuleFor(x => x.Amount)
            .NotNull()
            .WithMessage("amount is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Amount!.Value)
                    .GreaterThan(0)
                    .WithMessage("amount must be greater than 0.");
            });

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("currency is required.")
            .DependentRules(() =>
            {
                RuleFor(x => x.Currency)
                    .Must(Iso4217Currencies.IsValid)
                    .WithMessage("currency must be a valid ISO 4217 code.");
            });

        RuleFor(x => x.Timestamp)
            .NotNull()
            .WithMessage("timestamp is required.");
    }
}
