using LoyaltyPoints.Domain.Entities;

namespace LoyaltyPoints.Application.Abstractions;

public sealed record AwardProcessingResult(Award Award, bool IsNew);
