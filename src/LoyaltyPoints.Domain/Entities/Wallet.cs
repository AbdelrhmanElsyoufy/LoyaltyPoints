namespace LoyaltyPoints.Domain.Entities;

public sealed class Wallet
{
    public string Id { get; }

    public int Balance { get; private set; }

    public Wallet(string id, int openingBalance)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (openingBalance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(openingBalance));
        }

        Id = id;
        Balance = openingBalance;
    }

    public void AddPoints(int points)
    {
        if (points is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(points), "Points must be between 1 and 1000.");
        }

        Balance = checked(Balance + points);
    }
}
