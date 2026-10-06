using System.Diagnostics;
using System.Text.Json.Serialization;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using LifeCore.Contracts;
using LifeCore.Data;
using LifeCore.Web.Components;
using LifeCore.Web.Endpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection("Seed"));
builder.Services.Configure<PerfOptions>(builder.Configuration.GetSection("Perf"));
builder.Services.AddSingleton<SeedState>();
builder.Services.AddProblemDetails();
builder.Services.AddMemoryCache();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddRazorComponents().AddInteractiveWebAssemblyComponents();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<LifeCoreDbContext>(options =>
{
    var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("LifeCore"), sql => sql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlite(builder.Configuration.GetConnectionString("LifeCore") ?? "Data Source=lifecore.db");
    }
});
builder.Services.AddScoped<LifeCoreSeeder>();
builder.Services.AddHostedService<SeedHostedService>();
builder.Services.AddHealthChecks().AddDbContextCheck<LifeCoreDbContext>("db").AddCheck<SeededHealthCheck>("seeded");
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var feature = context.Features.Get<IExceptionHandlerFeature>();
    await Results.Problem(title: "Unhandled server error", detail: app.Environment.IsDevelopment() ? feature?.Error.Message : null, statusCode: 500).ExecuteAsync(context);
}));
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue(ApiRoutes.CorrelationHeader, out var value) && !string.IsNullOrWhiteSpace(value) ? value.ToString() : Guid.NewGuid().ToString("N");
    context.Response.Headers[ApiRoutes.CorrelationHeader] = correlationId;
    context.Response.Headers["X-Perf-Optimized"] = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<PerfOptions>>().CurrentValue.UseOptimizedQueries.ToString().ToLowerInvariant();
    using var scope = app.Logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId, ["Persona"] = context.Request.Headers[ApiRoutes.PersonaHeader].ToString() });
    Activity.Current?.SetTag("correlation.id", correlationId);
    Activity.Current?.SetTag("persona", context.Request.Headers[ApiRoutes.PersonaHeader].ToString());
    await next();
});

if (app.Environment.IsDevelopment()) app.UseWebAssemblyDebugging(); else app.UseHsts();
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments(ApiRoutes.Base), branch => branch.UseHttpsRedirection());
app.MapOpenApi();
app.MapScalarApiReference("/scalar");
app.MapGet(ApiRoutes.HealthLive, () => Results.Ok(new { status = "Healthy" }));
app.MapHealthChecks(ApiRoutes.HealthReady, new HealthCheckOptions { ResponseWriter = async (ctx, report) => await ctx.Response.WriteAsJsonAsync(new { status = report.Status.ToString(), checks = report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString()) }) });
app.MapLifeCoreEndpoints();
if (app.Configuration.GetValue("Admin:EnableReset", false)) app.MapLifeCoreAdmin();
app.Map(ApiRoutes.Base + "/{**catchall}", () => Results.NotFound(new ProblemDetails { Title = "Not found", Status = 404 }));

app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LifeCore.Web.Client._Imports).Assembly);

app.Run();

public partial class Program;

public sealed class SeedState { public volatile bool Ready; public string? Error; }

public sealed class SeedHostedService(IServiceProvider services, SeedState state, Microsoft.Extensions.Options.IOptions<SeedOptions> options, ILogger<SeedHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<LifeCoreDbContext>();
            // EnsureCreated is intentional for this two-provider demo; migrations would duplicate SQLite and Azure SQL flows.
            await db.Database.EnsureCreatedAsync(cancellationToken);
            if (options.Value.OnStartup) await scope.ServiceProvider.GetRequiredService<LifeCoreSeeder>().EnsureSeededAsync(cancellationToken);
            state.Ready = true;
        }
        catch (Exception ex) { state.Error = ex.Message; logger.LogError(ex, "Database initialization failed"); throw; }
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class SeededHealthCheck(SeedState state) : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.Ready ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy() : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(state.Error ?? "Seeding not complete"));
}
