using System.Text.Json;
using LoyaltyPoints.Api.Contracts.Requests;

namespace LoyaltyPoints.Api.Validation;

public static class AwardRequestValidator
{
    public static AwardRequest? Validate(JsonElement body, out string? error)
    {
        error = null;

        if (body.ValueKind != JsonValueKind.Object)
        {
            error = "The request body must be a JSON object.";
            return null;
        }

        var errors = new List<string>();
        var sourceId = ReadIdentifier(body, "sourceId", errors);
        var externalTransactionId = ReadIdentifier(body, "externalTransactionId", errors);
        var walletId = ReadIdentifier(body, "walletId", errors);
        var points = ReadPoints(body, errors);

        if (errors.Count > 0 ||
            sourceId is null ||
            externalTransactionId is null ||
            walletId is null ||
            points is null)
        {
            error = string.Join(" ", errors);
            return null;
        }

        return new AwardRequest(
            sourceId,
            externalTransactionId,
            walletId,
            points.Value);
    }

    private static string? ReadIdentifier(
        JsonElement body,
        string propertyName,
        ICollection<string> errors)
    {
        if (!body.TryGetProperty(propertyName, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            errors.Add($"'{propertyName}' is required and must be a string.");
            return null;
        }

        var identifier = value.GetString();
        if (string.IsNullOrEmpty(identifier) ||
            identifier.Length > 64 ||
            identifier.Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '_' and not '-'))
        {
            errors.Add(
                $"'{propertyName}' must contain 1-64 ASCII letters, digits, underscores, or hyphens.");
            return null;
        }

        return identifier;
    }

    private static int? ReadPoints(JsonElement body, ICollection<string> errors)
    {
        if (!body.TryGetProperty("points", out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var points))
        {
            errors.Add("'points' is required and must be an integer from 1 to 1000.");
            return null;
        }

        if (points is < 1 or > 1000)
        {
            errors.Add("'points' must be an integer from 1 to 1000.");
            return null;
        }

        return points;
    }
}
