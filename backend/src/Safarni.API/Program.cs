
using Safarni.API.Middleware;
using Safarni.API.Extensions;
using Safarni.API.Services;

using Safarni.Application;
using Safarni.Application.Abstractions.Authentication;

using Safarni.Infrastructure;
using Safarni.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;


// ================= ChatBot / RAG Imports =================

using Safarni.Application.Behaviors;
using ChatBotApi.Application.Common.Interfaces;
using ChatBotApi.Application.Features.Chat.Services;

using ChatBotApi.Infrastructure.AI;
using ChatBotApi.Infrastructure.Persistence.Repositories;
using ChatBotApi.Infrastructure.Persistence;
using MediatR;
using FluentValidation;



LoadDotEnv();

var builder = WebApplication.CreateBuilder(args);


// ================= Original Safarni Configuration =================


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });


builder.Services.AddCors(options =>
{
    var configuredOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    var allowedOrigins = configuredOrigins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.Trim().TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    options.AddPolicy("AllowFrontend", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }

        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();


builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    // Fix duplicate DTO/schema names such as:
    // ChatBotApi.SendMessageRequest
    // Safarni.API.Controllers.PackageBookingMessagesController.SendMessageRequest
    options.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

    options.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter a valid JWT access token."
        });

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    }
                },
                []
            }
        });
});


builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);



// ======================================================
// ================= ChatBot + RAG Services ==============
// ======================================================


// HTTP Client Factory
builder.Services.AddHttpClient();


// Repository Pattern
builder.Services.AddScoped<IChatRepository, ChatRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


// AI Providers (Groq)
builder.Services.AddScoped<IAIProvider>(
    services =>
        new GroqProvider(
            services.GetRequiredService<
                IHttpClientFactory>(),
            services.GetRequiredService<
                IConfiguration>(),
            services.GetRequiredService<
                ILogger<GroqProvider>>(),
            "GroqChat"));

builder.Services.AddScoped<IItineraryAIProvider>(
    services =>
        new GroqProvider(
            services.GetRequiredService<
                IHttpClientFactory>(),
            services.GetRequiredService<
                IConfiguration>(),
            services.GetRequiredService<
                ILogger<GroqProvider>>(),
            "GroqItinerary"));

builder.Services.UseGeminiForItineraryGeneration();

// Embeddings

builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();


// Qdrant Vector Database
builder.Services.AddHttpClient<IQdrantService, QdrantService>();


// Indexing
builder.Services.AddScoped<IIndexingService, IndexingService>();


// Prompt Builder
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();


// Database Seeder
builder.Services.AddScoped<ChatDbSeeder>();

builder.Services.AddScoped<AdminAccountSeeder>();


// Intent Detection
builder.Services.AddScoped<IIntentDetectionService, IntentDetectionService>();
builder.Services.AddScoped<IChatScopeClassifier, ChatScopeClassifier>();


// Google Places API
builder.Services.AddHttpClient<
    IGooglePlacesService,
    GooglePlacesService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(8);
    });



// ======================================================
// ================= Application Build ===================
// ======================================================


var app = builder.Build();



// Original Exception Middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}



app.UseDefaultFiles();
var persistentUploadsRoot = Path.Combine(app.Environment.ContentRootPath, "App_Data", "uploads");
Directory.CreateDirectory(persistentUploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(persistentUploadsRoot),
    RequestPath = "/uploads"
});
app.UseStaticFiles();



app.UseCors("AllowFrontend");



app.UseHttpsRedirection();



app.UseAuthentication();

app.UseAuthorization();



app.MapControllers();
app.MapFallbackToFile("index.html");




// ======================================================
// ================= Initialization ======================
// ======================================================


await app.Services.InitializeAsync();



using (var scope = app.Services.CreateScope())
{

    // ChatBot Seeder
    var seeder = scope.ServiceProvider
        .GetRequiredService<ChatDbSeeder>();

    await seeder.SeedAsync();


    // Controlled first admin account (Admin:Email / Admin:Password)
    var adminSeeder = scope.ServiceProvider
        .GetRequiredService<AdminAccountSeeder>();

    await adminSeeder.SeedAsync();

    var marketplaceSeeder = scope.ServiceProvider
        .GetRequiredService<MarketplaceTestDataSeeder>();

    await marketplaceSeeder.SeedAsync();

    var pricingSeeder = scope.ServiceProvider
        .GetRequiredService<PricingCatalogSeeder>();

    await pricingSeeder.SeedAsync();



    // RAG Qdrant Auto Indexing

    var qdrantService = scope.ServiceProvider
        .GetRequiredService<IQdrantService>();


    var indexingService = scope.ServiceProvider
        .GetRequiredService<IIndexingService>();


    try
    {

        if (await qdrantService.IsHealthyAsync())
        {
            bool wasCreatedOrRecreated = await qdrantService.CreateCollectionAsync();
            var indexedPointCount = await qdrantService.GetPointCountAsync();
            var autoIndexOnStartup = !bool.TryParse(
                    builder.Configuration["RAG:AutoIndexOnStartup"],
                    out var configuredAutoIndex) ||
                configuredAutoIndex;

            if (autoIndexOnStartup && (wasCreatedOrRecreated || indexedPointCount == 0))
            {
                Console.WriteLine("Qdrant collection is new or empty. Starting full RAG indexing...");
                await indexingService.IndexAllAsync();
                Console.WriteLine("RAG indexing completed successfully.");
            }
            else
            {
                Console.WriteLine($"Qdrant collection is ready with {indexedPointCount} indexed points.");
            }
        }
        else
        {
            Console.WriteLine(
                "Warning: Qdrant is not running.");
        }

    }
    catch(Exception ex)
    {

        Console.WriteLine(
            $"RAG initialization failed: {ex.Message}");

    }

}



app.Run();

static void LoadDotEnv()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory != null)
    {
        var envPath = Path.Combine(directory.FullName, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = trimmed.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = trimmed[..separatorIndex].Trim();
                var value = trimmed[(separatorIndex + 1)..].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(key) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }

        directory = directory.Parent;
    }
}
