using LoyaltyPoints.Domain.Entities;
using LoyaltyPoints.Domain.ValueObjects;

namespace LoyaltyPoints.Application.Abstractions;

public interface IWalletStore
{
    Task<Wallet?> FindWalletAsync(
        string walletId,
        CancellationToken cancellationToken = default);

    Task<AwardProcessingResult> ProcessAwardAsync(
        string walletId,
        AwardIdentity identity,
        int points,
        CancellationToken cancellationToken = default);
}
