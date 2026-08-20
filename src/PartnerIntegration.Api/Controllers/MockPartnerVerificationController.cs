using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.Infrastructure.PartnerVerification;

namespace PartnerIntegration.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/mock/partners")]
public sealed class MockPartnerVerificationController : ControllerBase
{
    private readonly FlakyPartnerVerificationService _verificationService;

    public MockPartnerVerificationController(FlakyPartnerVerificationService verificationService)
    {
        _verificationService = verificationService;
    }

    [HttpGet("{partnerId}/verify")]
    [ProducesResponseType(typeof(Application.Contracts.PartnerVerificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public IActionResult Verify(string partnerId)
    {
        var result = _verificationService.Verify(partnerId);
        return Ok(result);
    }
}
