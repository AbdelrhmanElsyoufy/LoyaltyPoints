namespace LoyaltyPoints.Domain.Exceptions;

public sealed class ConflictException : InvalidOperationException
{
    public ConflictException(string message)
        : base(message)
    {
    }
}
