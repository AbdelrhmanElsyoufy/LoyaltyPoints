using LoyaltyPoints.Application.Abstractions;
using LoyaltyPoints.Domain.Entities;

namespace LoyaltyPoints.Application.Services;

public sealed class GetWalletService
{
    private readonly IWalletStore _walletStore;

    public GetWalletService(IWalletStore walletStore)
    {
        _walletStore = walletStore ?? throw new ArgumentNullException(nameof(walletStore));
    }

    public Task<Wallet?> GetWalletAsync(
        string? walletId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletId))
        {
            return Task.FromResult<Wallet?>(null);
        }

        return _walletStore.FindWalletAsync(walletId, cancellationToken);
    }
}
