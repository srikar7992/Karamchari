using System.Globalization;
using Karamchari.Core.DependencyInjection;
using Karamchari.Identity.Infrastructure;
using Karamchari.Worker.DependencyInjection;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// 1. Logging
builder.Services.AddSerilog((sp, cfg) =>
{
    cfg.ReadFrom.Configuration(builder.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
});

// 2. Identity & Security (For background job context)
builder.Services.AddKaramchariIdentity(builder.Configuration);

// 3. Infrastructure & Core
builder.Services.AddKaramchariCore(builder.Configuration);

// 4. Messaging & Async Processing (Worker Mode)
builder.Services.AddKaramchariWorkerMessaging(builder.Configuration, builder.Environment);

// 5. Consolidate all Background Services here
builder.Services.AddKaramchariWorkerPool(builder.Configuration);

var host = builder.Build();

host.Run();
