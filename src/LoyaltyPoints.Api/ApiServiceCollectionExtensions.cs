using LoyaltyPoints.Api.Contracts.Responses;
using LoyaltyPoints.Application.Abstractions;
using LoyaltyPoints.Application.Services;
using LoyaltyPoints.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace LoyaltyPoints.Api;

public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddLoyaltyPointsApi(this IServiceCollection services)
    {
        services
            .AddControllers()
            .ConfigureApiBehaviorOptions(ConfigureApiBehavior);

        services.AddSingleton<IWalletStore, InMemoryWalletStore>();
        services.AddScoped<AwardPointsService>();
        services.AddScoped<GetWalletService>();
        return services;
    }

    private static void ConfigureApiBehavior(ApiBehaviorOptions options)
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = context.ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error));

            return new BadRequestObjectResult(
                new ErrorResponse(message ?? "The request body is invalid."));
        };
    }
}
