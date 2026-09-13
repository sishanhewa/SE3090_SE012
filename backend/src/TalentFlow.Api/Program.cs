using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TalentFlow.Application;
using TalentFlow.Application.Common;
using TalentFlow.Application.Interfaces.Services;
using TalentFlow.Infrastructure;
using TalentFlow.Infrastructure.Middleware;
using TalentFlow.Infrastructure.Persistence;
using TalentFlow.Infrastructure.Services;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // --- Service Registration ---

    // Application layer
    builder.Services.AddApplicationServices();

    // Infrastructure layer (DbContext, Identity, Repositories)
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Auth service
    builder.Services.AddScoped<IAuthService, AuthService>();

    // JWT Settings
    var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
        ?? throw new InvalidOperationException("JwtSettings is not configured in appsettings.json");

    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

    // JWT Authentication
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    builder.Services.AddAuthorization();

    // CORS - Allow React (localhost:5173) and Flutter
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowClients", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",   // React dev server
                    "http://localhost:3000",   // React alternative
                    "http://localhost:8080",   // Flutter web
                    "http://10.0.2.2:5000"    // Android emulator
                )
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    // Controllers
    builder.Services.AddControllers();

    // Swagger with JWT support
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "TalentFlow API",
            Version = "v1",
            Description = "AI-Powered Recruitment and Employee Management Platform API"
        });

        // Add JWT auth to Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    // --- Middleware Pipeline ---

    // Request logging (outermost — catches all requests)
    app.UseMiddleware<RequestLoggingMiddleware>();

    // Exception handling (catches exceptions from everything below)
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Swagger (development only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TalentFlow API v1");
        });
    }

    app.UseHttpsRedirection();
    app.UseCors("AllowClients");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // Seed database
    using (var scope = app.Services.CreateScope())
    {
        await DataSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
