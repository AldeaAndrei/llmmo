using llmmo.Api;
using llmmo.Api.GameRules;
using llmmo.Auth;
using llmmo.Data;
using llmmo.Tick;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load the live balance profile (costs, durations, production, scaling curves) from a
// JSON file produced by the BalanceSim search. Falls back to built-in defaults if absent.
var balanceProfilePath = builder.Configuration.GetValue<string>("BalanceProfilePath");
if (string.IsNullOrWhiteSpace(balanceProfilePath))
{
    balanceProfilePath = Path.Combine(builder.Environment.ContentRootPath, "Config", "balance-profile.json");
}

if (BalanceProfile.TryLoadActiveFromFile(balanceProfilePath))
{
    var active = BalanceProfile.Active;
    Console.WriteLine($"[balance] Loaded profile from '{balanceProfilePath}' " +
        $"(scaling={active.ScalingMode}, productionPerLevel={active.ProductionPerLevel}, baseUpgradeTicks={active.BaseUpgradeTicks}).");
}
else
{
    Console.WriteLine($"[balance] No profile at '{balanceProfilePath}'; using built-in default balance.");
}

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.Configure<TickOptions>(options =>
{
    options.TickIntervalSeconds = builder.Configuration.GetValue("TickIntervalSeconds", 5);
});
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AgentManagementService>();
builder.Services.AddScoped<ActionSubmissionService>();
builder.Services.AddScoped<AttackSubmissionService>();
builder.Services.AddScoped<PossibleActionsService>();
builder.Services.AddScoped<IntelService>();
builder.Services.AddScoped<DiplomacyService>();
builder.Services.AddScoped<ActionCompleter>();
builder.Services.AddScoped<CombatResolver>();
builder.Services.AddScoped<AttackMovementService>();
builder.Services.AddScoped<CityEconomyService>();
builder.Services.AddScoped<ProductionService>();
builder.Services.AddScoped<TroopUpkeepService>();
builder.Services.AddScoped<TickService>();
builder.Services.AddHostedService<TickBackgroundService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
{
    corsOrigins = new[] { "http://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// Trust X-Forwarded-* from the reverse proxy (cloudflared / Cloudflare Tunnel)
// so Request.IsHttps reflects the public HTTPS scheme and Secure cookies work.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply any pending EF Core migrations on startup so the schema is ready in
// fresh container deployments. Retries briefly while the database warms up.
ApplyMigrations(app);

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseMiddleware<AuthMiddleware>();

app.MapGet("/", () => Results.Ok(new { name = "llmmo", status = "ok" }));

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/db", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Name == "postgres",
});

app.MapApiEndpoints();

app.Run();

static void ApplyMigrations(WebApplication app)
{
    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            app.Logger.LogWarning(
                ex,
                "Database not ready (attempt {Attempt}/{MaxAttempts}); retrying in 3s.",
                attempt,
                maxAttempts);
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }
    }
}
