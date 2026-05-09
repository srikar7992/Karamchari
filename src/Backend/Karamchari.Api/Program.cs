using Karamchari.Api.DependencyInjection;
using Karamchari.Core.DependencyInjection;
using Karamchari.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging & Observability
builder.AddKaramchariLogging();

// 2. Identity & Security (Enterprise Foundation)
builder.Services.AddKaramchariIdentity(builder.Configuration);
builder.Services.AddAuthorization();

// 3. Infrastructure & Core
builder.Services.AddKaramchariCore(builder.Configuration);
builder.Services.AddKaramchariInfrastructure(builder.Configuration);
builder.Services.AddKaramchariHealthChecks(builder.Configuration);
builder.Services.AddKaramchariResilience();

// 4. Messaging & Async Processing
builder.Services.AddKaramchariMassTransit(builder.Configuration, builder.Environment);

var app = builder.Build();

// 5. Request Pipeline
app.UseExceptionHandler(opt => { }); // Uses IExceptionHandler registered in services

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// 6. Endpoints & Health
app.MapKaramchariHealthChecks();
app.MapKaramchariEndpoints();

app.Run();

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public partial class Program { }

