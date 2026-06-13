using llmmo.Api;
using llmmo.Auth;
using llmmo.Data;
using llmmo.Tick;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<ActionCompleter>();
builder.Services.AddScoped<ProductionService>();
builder.Services.AddScoped<TickService>();
builder.Services.AddHostedService<TickBackgroundService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres");

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
