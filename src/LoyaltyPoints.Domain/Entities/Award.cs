using LoyaltyPoints.Domain.ValueObjects;

namespace LoyaltyPoints.Domain.Entities;

public sealed class Award
{
    public Guid AwardId { get; }

    public AwardIdentity Identity { get; }

    public string SourceId => Identity.SourceId;

    public string ExternalTransactionId => Identity.ExternalTransactionId;

    public string WalletId { get; }

    public int Points { get; }

    public Award(
        Guid awardId,
        AwardIdentity identity,
        string walletId,
        int points)
    {
        if (awardId == Guid.Empty)
        {
            throw new ArgumentException("Award ID is required.", nameof(awardId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(walletId);

        if (points is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(points));
        }

        AwardId = awardId;
        Identity = identity;
        WalletId = walletId;
        Points = points;
    }
}
