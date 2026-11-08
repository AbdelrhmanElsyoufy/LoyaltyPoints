namespace LoyaltyPoints.Domain.Exceptions;

public sealed class WalletNotFoundException : KeyNotFoundException
{
    public WalletNotFoundException(string walletId)
        : base($"Wallet '{walletId}' was not found.")
    {
    }
}
