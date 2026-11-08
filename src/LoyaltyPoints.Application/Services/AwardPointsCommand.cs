namespace LoyaltyPoints.Application.Services;

public sealed record AwardPointsCommand(
    string SourceId,
    string ExternalTransactionId,
    string WalletId,
    int Points);
