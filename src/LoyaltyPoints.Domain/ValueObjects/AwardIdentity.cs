namespace LoyaltyPoints.Domain.ValueObjects;

public readonly record struct AwardIdentity(
    string SourceId,
    string ExternalTransactionId
);
