using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LoyaltyPoints.Api;
using LoyaltyPoints.Api.Contracts.Responses;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LoyaltyPoints.Tests;

public sealed class AwardsApiTests
{
    [Fact]
    public async Task PostAward_CreatesAwardAndUpdatesWallet()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var award = await response.Content.ReadFromJsonAsync<AwardResponse>();
        Assert.NotNull(award);
        Assert.NotEqual(Guid.Empty, award.AwardId);
        Assert.Equal("POS_A", award.SourceId);
        Assert.Equal("T1", award.ExternalTransactionId);
        Assert.Equal("W100", award.WalletId);
        Assert.Equal(25, award.Points);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_MatchingRetryReturnsOriginalAwardWithoutAddingPointsAgain()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var firstResponse = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        var firstAward = await firstResponse.Content.ReadFromJsonAsync<AwardResponse>();
        var retryResponse = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        var retryAward = await retryResponse.Content.ReadFromJsonAsync<AwardResponse>();

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        Assert.NotNull(firstAward);
        Assert.NotNull(retryAward);
        Assert.Equal(firstAward.AwardId, retryAward.AwardId);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_ConflictingRetryDoesNotChangeOriginalAwardOrWallet()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var originalResponse = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        var originalAward = await originalResponse.Content.ReadFromJsonAsync<AwardResponse>();
        var conflictResponse = await PostAwardAsync(client, "POS_A", "T1", "W404", 50);

        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
        Assert.Equal(50, await GetBalanceAsync(client, "W200"));
        Assert.NotNull(originalAward);

        var retryResponse = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        var retryAward = await retryResponse.Content.ReadFromJsonAsync<AwardResponse>();
        Assert.Equal(HttpStatusCode.OK, retryResponse.StatusCode);
        Assert.Equal(originalAward.AwardId, retryAward?.AwardId);
    }

    [Fact]
    public async Task PostAward_DifferentSourceCreatesDifferentIdentity()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var first = await PostAwardAsync(client, "POS_A", "T1", "W100", 10);
        var second = await PostAwardAsync(client, "POS_B", "T1", "W100", 15);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_UnknownWalletDoesNotConsumeIdentity()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var unknownWalletResponse = await PostAwardAsync(client, "POS_A", "T1", "W404", 25);
        var correctedResponse = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);

        Assert.Equal(HttpStatusCode.NotFound, unknownWalletResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, correctedResponse.StatusCode);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_AcceptsMaximumLengthIdentifiersAndMaximumPoints()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var response = await PostAwardAsync(
            client,
            new string('P', 64),
            new string('T', 64),
            "W100",
            1000);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1100, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_ValidatesFieldsBeforeCheckingExistingIdentityOrWallet()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        using var invalidDuplicate = await PostAwardAsync(client, "POS_A", "T1", "W100", 0);
        using var invalidUnknownWallet = await PostAwardAsync(client, "POS_B", "T2", "W404", 1001);

        Assert.Equal(HttpStatusCode.BadRequest, invalidDuplicate.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidUnknownWallet.StatusCode);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_RejectsMalformedJsonNonObjectsAndInvalidFieldsAsJson400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var invalidBodies = new[]
        {
            "{\"sourceId\":",
            "[]",
            "{\"sourceId\":1,\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":25}",
            "{\"sourceId\":\"PÖS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":25}",
            $"{{\"sourceId\":\"{new string('P', 65)}\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":25}}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":0}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":1001}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":25.5}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":\"25\"}"
        };

        foreach (var body in invalidBodies)
        {
            using var response = await PostRawAsync(client, body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            Assert.True(json.RootElement.TryGetProperty("error", out var error));
            Assert.False(string.IsNullOrWhiteSpace(error.GetString()));
        }
    }

    [Fact]
    public async Task PostAward_ConcurrentMatchingRequestsCreateOneAwardAndReturnRetries()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20)
                .Select(_ => PostAwardAsync(client, "POS_A", "T1", "W100", 25)));

        Assert.Equal(1, responses.Count(response => response.StatusCode == HttpStatusCode.Created));
        Assert.Equal(19, responses.Count(response => response.StatusCode == HttpStatusCode.OK));
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_DistinctConcurrentAwardsAllIncreaseWalletBalance()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 24)
                .Select(index => PostAwardAsync(client, "POS_A", $"T{index}", "W100", 1)));

        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Created, response.StatusCode));
        Assert.Equal(124, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_ConcurrentConflictingRequestsProduceOneAwardAndOneConflict()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();
        using var start = new ManualResetEventSlim(false);

        var first = Task.Run(async () =>
        {
            start.Wait();
            return await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        });
        var second = Task.Run(async () =>
        {
            start.Wait();
            return await PostAwardAsync(client, "POS_A", "T1", "W100", 30);
        });
        start.Set();

        var responses = await Task.WhenAll(first, second);

        var created = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var winner = await created.Content.ReadFromJsonAsync<AwardResponse>();
        Assert.NotNull(winner);
        Assert.Equal(100 + winner.Points, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task GetWallet_ReturnsSeededBalancesAndNotFoundForUnknownWallet()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var first = await client.GetFromJsonAsync<WalletResponse>("/wallets/W100");
        var second = await client.GetFromJsonAsync<WalletResponse>("/wallets/W200");
        var missing = await client.GetAsync("/wallets/W404");

        Assert.Equal(new WalletResponse("W100", 100), first);
        Assert.Equal(new WalletResponse("W200", 50), second);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PostAward_RetryWithDifferentExistingWalletReturns409AndChangesNoBalance()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        using var conflict = await PostAwardAsync(client, "POS_A", "T1", "W200", 25);

        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal(125, await GetBalanceAsync(client, "W100"));
        Assert.Equal(50, await GetBalanceAsync(client, "W200"));
    }

    [Fact]
    public async Task PostAward_IdentifiersAreCaseSensitive()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var upper = await PostAwardAsync(client, "POS_A", "T1", "W100", 25);
        using var lower = await PostAwardAsync(client, "pos_a", "t1", "W100", 25);

        Assert.Equal(HttpStatusCode.Created, upper.StatusCode);
        Assert.Equal(HttpStatusCode.Created, lower.StatusCode);
        Assert.Equal(150, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task PostAward_RejectsMissingAndNullFieldsAsJson400()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        var invalidBodies = new[]
        {
            "{}",
            "null",
            "{\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":25}",
            "{\"sourceId\":\"POS\",\"walletId\":\"W100\",\"points\":25}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"points\":25}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\"}",
            "{\"sourceId\":null,\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":25}",
            "{\"sourceId\":\"POS\",\"externalTransactionId\":\"T1\",\"walletId\":\"W100\",\"points\":null}"
        };

        foreach (var body in invalidBodies)
        {
            using var response = await PostRawAsync(client, body);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            Assert.True(json.RootElement.TryGetProperty("error", out var error));
            Assert.False(string.IsNullOrWhiteSpace(error.GetString()));
        }

        Assert.Equal(100, await GetBalanceAsync(client, "W100"));
    }

    [Fact]
    public async Task GetWallet_WhitespaceIdReturnsJson404()
    {
        using var factory = new WebApplicationFactory<Program>();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/wallets/%20");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(json.RootElement.TryGetProperty("error", out _));
    }

    private static Task<HttpResponseMessage> PostAwardAsync(
        HttpClient client,
        string sourceId,
        string externalTransactionId,
        string walletId,
        int points)
    {
        return client.PostAsJsonAsync("/awards", new
        {
            sourceId,
            externalTransactionId,
            walletId,
            points
        });
    }

    private static Task<HttpResponseMessage> PostRawAsync(HttpClient client, string body)
    {
        return client.PostAsync(
            "/awards",
            new StringContent(body, Encoding.UTF8, "application/json"));
    }

    private static async Task<int> GetBalanceAsync(HttpClient client, string walletId)
    {
        var wallet = await client.GetFromJsonAsync<WalletResponse>($"/wallets/{walletId}");
        Assert.NotNull(wallet);
        return wallet.Balance;
    }

}
