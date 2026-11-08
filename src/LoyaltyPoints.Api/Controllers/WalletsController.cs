using LoyaltyPoints.Api.Contracts.Responses;
using LoyaltyPoints.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoyaltyPoints.Api.Controllers;

[ApiController]
[Route("wallets")]
public sealed class WalletsController : ControllerBase
{
    private readonly GetWalletService _getWalletService;

    public WalletsController(GetWalletService getWalletService)
    {
        _getWalletService = getWalletService;
    }

    [HttpGet("{walletId}")]
    public async Task<IActionResult> GetById(
        string? walletId,
        CancellationToken cancellationToken)
    {
        var wallet = await _getWalletService.GetWalletAsync(walletId, cancellationToken);
        return wallet is null
            ? NotFound(new ErrorResponse($"Wallet '{walletId}' was not found."))
            : Ok(new WalletResponse(wallet.Id, wallet.Balance));
    }
}
