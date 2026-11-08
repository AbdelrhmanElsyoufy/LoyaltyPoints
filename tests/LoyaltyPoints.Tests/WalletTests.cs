using LoyaltyPoints.Domain.Entities;

namespace LoyaltyPoints.Tests;

public class WalletTests
{
    [Fact]
    public void AddPoints_ShouldIncreaseBalance()
    {
        var wallet = new Wallet("W100", 100);

        wallet.AddPoints(25);

        Assert.Equal(125, wallet.Balance);
    }

    [Fact]
    public void AddPoints_ShouldRejectZero()
    {
        var wallet = new Wallet("W100", 100);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => wallet.AddPoints(0));
    }

    [Fact]
    public void AddPoints_ShouldRejectMoreThan1000()
    {
        var wallet = new Wallet("W100", 100);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => wallet.AddPoints(1001));
    }

    [Fact]
    public void AddPoints_ShouldPreventIntegerOverflow()
    {
        var wallet = new Wallet("W100", int.MaxValue);

        Assert.Throws<OverflowException>(
            () => wallet.AddPoints(1));
    }
}