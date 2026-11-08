using LoyaltyPoints.Application.Abstractions;
using LoyaltyPoints.Domain.Entities;
using LoyaltyPoints.Domain.ValueObjects;

namespace LoyaltyPoints.Application.Services;

public sealed class AwardPointsService
{
    private readonly IWalletStore _walletStore;

    public AwardPointsService(IWalletStore walletStore)
    {
        _walletStore = walletStore ?? throw new ArgumentNullException(nameof(walletStore));
    }

    public Task<AwardProcessingResult> AwardAsync(
        AwardPointsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        return _walletStore.ProcessAwardAsync(
            command.WalletId,
            new AwardIdentity(command.SourceId, command.ExternalTransactionId),
            command.Points,
            cancellationToken);
    }
}
