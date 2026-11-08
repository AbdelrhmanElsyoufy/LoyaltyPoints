using LoyaltyPoints.Domain.Entities;

namespace LoyaltyPoints.Api.Contracts.Responses;

public sealed record AwardResponse(
    Guid AwardId,
    string SourceId,
    string ExternalTransactionId,
    string WalletId,
    int Points)
{
    public static AwardResponse From(Award award)
    {
        ArgumentNullException.ThrowIfNull(award);

        return new AwardResponse(
            award.AwardId,
            award.SourceId,
            award.ExternalTransactionId,
            award.WalletId,
            award.Points);
    }
}
