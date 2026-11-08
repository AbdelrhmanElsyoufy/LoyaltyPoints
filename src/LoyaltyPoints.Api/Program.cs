using LoyaltyPoints.Api;
using LoyaltyPoints.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLoyaltyPointsApi();

var app = builder.Build();

app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.MapControllers();

app.Run();
