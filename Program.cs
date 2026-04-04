using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using HealthRecord.API.Common.Helpers;
using HealthRecord.API.Common.Middleware;
using HealthRecord.API.Infrastructure.Data;
using HealthRecord.API.Services;
using HealthRecord.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Zeabur 透過 PORT 環境變數指定 port
    var port = Environment.GetEnvironmentVariable("PORT");
    if (port != null)
        builder.WebHost.UseUrls($"http://+:{port}");

    // Serilog
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .Enrich.FromLogContext());

    // CORS — 從設定檔或環境變數讀取允許的 origins
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod());
    });

    // Database
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Missing ConnectionStrings:DefaultConnection. Set env var ConnectionStrings__DefaultConnection");
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 0))));

    // JWT
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var secret = jwtSection["Secret"]!;
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSection["Issuer"],
                ValidAudience = jwtSection["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };
        });
    builder.Services.AddAuthorization();

    // Controllers + FluentValidation
    builder.Services.AddControllers();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // Services
    builder.Services.AddScoped<JwtHelper>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IProfileService, ProfileService>();
    builder.Services.AddScoped<IBloodPressureService, BloodPressureService>();
    builder.Services.AddScoped<ILabService, LabService>();
    builder.Services.AddScoped<IHealthRecordService, HealthRecordService>();
    builder.Services.AddScoped<IMedicationService, MedicationService>();
    builder.Services.AddScoped<INhiImportService, NhiImportService>();
    builder.Services.AddScoped<IUserLabItemService, UserLabItemService>();
    builder.Services.AddScoped<IVisitService, VisitService>();
    builder.Services.AddScoped<IVisitRelationService, VisitRelationService>();

    // Swagger (dev only)
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Auto-apply migrations (Zeabur 部署時自動執行，本地 Development ���過)
    if (!app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }

    // Seed test data (Development only, idempotent)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DbSeeder.SeedAsync(dbContext);
    }

    app.UseCors("AllowFrontend"); // Must be before auth middleware for preflight OPTIONS

    app.UseMiddleware<ExceptionMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
