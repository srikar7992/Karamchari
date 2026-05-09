using Karamchari.Api.DependencyInjection;
using Karamchari.Core.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// 1. Logging & Observability
builder.AddKaramchariLogging();

// 2. Infrastructure & Core
builder.Services.AddKaramchariCore(builder.Configuration);
builder.Services.AddKaramchariInfrastructure(builder.Configuration);
builder.Services.AddKaramchariHealthChecks(builder.Configuration);
builder.Services.AddKaramchariResilience();

// 3. Messaging & Async Processing
builder.Services.AddKaramchariMassTransit(builder.Configuration, builder.Environment);

// 4. Security (Phase 1B: Hardening)
builder.Services.AddAuthentication("DevelopmentTenant")
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, Karamchari.Api.Security.DevelopmentTenantAuthenticationHandler>("DevelopmentTenant", null);

builder.Services.AddAuthorization();

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
