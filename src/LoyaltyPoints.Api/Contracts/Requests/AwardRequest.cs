namespace LoyaltyPoints.Api.Contracts.Requests;

public sealed record AwardRequest(
    string SourceId,
    string ExternalTransactionId,
    string WalletId,
    int Points);
