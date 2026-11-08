using LoyaltyPoints.Application.Abstractions;
using LoyaltyPoints.Domain.Entities;
using LoyaltyPoints.Domain.Exceptions;
using LoyaltyPoints.Domain.ValueObjects;

namespace LoyaltyPoints.Infrastructure.Persistence;

public sealed class InMemoryWalletStore : IWalletStore
{
    private readonly Dictionary<string, Wallet> _wallets = new(StringComparer.Ordinal);
    private readonly Dictionary<AwardIdentity, Award> _awards = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    public InMemoryWalletStore()
    {
        _wallets.Add("W100", new Wallet("W100", 100));
        _wallets.Add("W200", new Wallet("W200", 50));
    }

    public async Task<Wallet?> FindWalletAsync(
        string walletId,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _wallets.TryGetValue(walletId, out var wallet)
                ? new Wallet(wallet.Id, wallet.Balance)
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<AwardProcessingResult> ProcessAwardAsync(
        string walletId,
        AwardIdentity identity,
        int points,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletId);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.SourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(identity.ExternalTransactionId);

        if (points is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be between 1 and 1000.");
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_awards.TryGetValue(identity, out var existingAward))
            {
                if (existingAward.WalletId != walletId || existingAward.Points != points)
                {
                    throw new ConflictException($"Award identity '{identity}' already exists with a conflicting payload.");
                }

                return new AwardProcessingResult(existingAward, IsNew: false);
            }

            if (!_wallets.TryGetValue(walletId, out var wallet))
            {
                throw new WalletNotFoundException(walletId);
            }

            var award = new Award(Guid.NewGuid(), identity, walletId, points);
            wallet.AddPoints(points);
            _awards[identity] = award;
            return new AwardProcessingResult(award, IsNew: true);
        }
        finally
        {
            _gate.Release();
        }
    }
}
