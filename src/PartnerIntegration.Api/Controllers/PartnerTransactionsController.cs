using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Application.Contracts;
using PartnerIntegration.Application.Services;
using PartnerIntegration.Api.Security;

namespace PartnerIntegration.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = ApiKeyOptions.SchemeName)]
[Route("api/v1/partner/transactions")]
public sealed class PartnerTransactionsController : ControllerBase
{
    private readonly IValidator<PartnerTransactionRequest> _validator;
    private readonly PartnerTransactionProcessor _processor;

    public PartnerTransactionsController(
        IValidator<PartnerTransactionRequest> validator,
        PartnerTransactionProcessor processor)
    {
        _validator = validator;
        _processor = processor;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PartnerTransactionAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(
        [FromBody] PartnerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await _processor.ProcessAsync(request, cancellationToken);

        return result.Status switch
        {
            ProcessTransactionStatus.Accepted => Accepted(
                $"/api/v1/partner/transactions/{result.TransactionReference}",
                new PartnerTransactionAcceptedResponse(
                    result.TransactionReference!,
                    "Accepted",
                    result.Message)),
            ProcessTransactionStatus.PartnerNotVerified => UnprocessableEntity(ToProblem(StatusCodes.Status422UnprocessableEntity, result.Message)),
            ProcessTransactionStatus.VerificationUnavailable => StatusCode(StatusCodes.Status503ServiceUnavailable, ToProblem(StatusCodes.Status503ServiceUnavailable, result.Message)),
            ProcessTransactionStatus.PublishFailed => StatusCode(StatusCodes.Status503ServiceUnavailable, ToProblem(StatusCodes.Status503ServiceUnavailable, result.Message)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, ToProblem(StatusCodes.Status500InternalServerError, "Unexpected processing result."))
        };
    }

    private ProblemDetails ToProblem(int status, string detail) => new()
    {
        Status = status,
        Title = status switch
        {
            StatusCodes.Status422UnprocessableEntity => "Partner not verified",
            StatusCodes.Status503ServiceUnavailable => "Service unavailable",
            _ => "Error"
        },
        Detail = detail,
        Instance = HttpContext.Request.Path
    };
}
