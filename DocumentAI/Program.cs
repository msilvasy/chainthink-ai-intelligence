using DocumentAI.Configuration;
using DocumentAI.Data;
using DocumentAI.Models;
using DocumentAI.Scheduler;
using DocumentAI.Services;
using DocumentAI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Services.Configure<OllamaSettings>(builder.Configuration.GetSection(OllamaSettings.SectionName));
builder.Services.Configure<ChromaSettings>(builder.Configuration.GetSection(ChromaSettings.SectionName));
builder.Services.Configure<ChunkingSettings>(builder.Configuration.GetSection(ChunkingSettings.SectionName));
builder.Services.Configure<SearchSettings>(builder.Configuration.GetSection(SearchSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// Add Entity Framework Core with SQL Server (or SQLite for development)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection"); 
  

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure Identity options
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
});

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();

// Validate JWT settings
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.SecretKey))
{
    throw new InvalidOperationException(
        $"JwtSettings is not configured properly in appsettings.json. " +
        $"Ensure the '{JwtSettings.SectionName}' section contains a valid 'SecretKey' (minimum 32 characters).");
}

if (jwtSettings.SecretKey.Length < 32)
{
    throw new InvalidOperationException(
        $"JwtSettings.SecretKey must be at least 32 characters long for security. " +
        $"Current length: {jwtSettings.SecretKey.Length}");
}

var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

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
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer ?? "DocumentAI",
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience ?? "DocumentAIUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// HTTP Clients
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>(client =>
{
    var settings = builder.Configuration.GetSection(OllamaSettings.SectionName).Get<OllamaSettings>() ?? new OllamaSettings();
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

builder.Services.AddHttpClient<ILlmService, LlmService>(client =>
{
    var settings = builder.Configuration.GetSection(OllamaSettings.SectionName).Get<OllamaSettings>() ?? new OllamaSettings();
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

builder.Services.AddHttpClient<IChromaService, ChromaService>(client =>
{
    var settings = builder.Configuration.GetSection(ChromaSettings.SectionName).Get<ChromaSettings>() ?? new ChromaSettings();
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// Services
builder.Services.AddSingleton<IDocumentParserService, DocumentParserService>();
builder.Services.AddSingleton<IChunkingService, ChunkingService>();
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IBm25Service, Bm25Service>();
builder.Services.AddScoped<IHybridSearchService, HybridSearchService>();
builder.Services.AddScoped<IQueryLogService, QueryLogService>();
builder.Services.AddScoped<ITokenService, TokenService>();

// Controllers
builder.Services.AddControllers();

// Timer
builder.Services.AddScoped<IMyScheduledJob, MyScheduledJob>();
builder.Services.AddHostedService<FiveMinuteTimerService>();
builder.Services.AddHostedService<EndpointCallerService>();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Document AI API",
        Version = "v1",
        Description = "AI-powered document processing and RAG system using local LLM (Ollama) and Chroma DB"
    });

    // Add JWT Authorization to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });




});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost4200", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200","https://chainthink.world")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

var app = builder.Build();

// Apply migrations and create database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        await dbContext.Database.MigrateAsync();
        app.Logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply database migrations");
    }

    var chromaService = scope.ServiceProvider.GetRequiredService<IChromaService>();
    try
    {
        await chromaService.InitializeAsync();
        app.Logger.LogInformation("Chroma DB collection initialized successfully");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Failed to initialize Chroma DB collection on startup. Will retry on first request.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Document AI API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost4200");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
