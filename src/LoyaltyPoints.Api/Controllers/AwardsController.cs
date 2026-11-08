using System.Text.Json;
using LoyaltyPoints.Api.Contracts.Requests;
using LoyaltyPoints.Api.Contracts.Responses;
using LoyaltyPoints.Api.Validation;
using LoyaltyPoints.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoyaltyPoints.Api.Controllers;

[ApiController]
[Route("awards")]
public sealed class AwardsController : ControllerBase
{
    private readonly AwardPointsService _awardPointsService;

    public AwardsController(AwardPointsService awardPointsService)
    {
        _awardPointsService = awardPointsService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        var request = AwardRequestValidator.Validate(body, out var error);
        if (request is null)
        {
            return BadRequest(new ErrorResponse(error ?? "The request body is invalid."));
        }

        var result = await _awardPointsService.AwardAsync(
            new AwardPointsCommand(
                request.SourceId,
                request.ExternalTransactionId,
                request.WalletId,
                request.Points),
            cancellationToken);
        var response = AwardResponse.From(result.Award);

        return result.IsNew
            ? StatusCode(StatusCodes.Status201Created, response)
            : Ok(response);
    }
}
