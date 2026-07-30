using System.Reflection;

using Safarni.API.Middleware;
using Safarni.API.Extensions;
using Safarni.API.Services;

using Safarni.Application;
using Safarni.Application.Abstractions.Authentication;

using Safarni.Infrastructure;
using Safarni.Infrastructure.Persistence;

using Microsoft.AspNetCore.Authentication.JwtBearer;
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


LoadLocalEnvironmentFile();

var builder = WebApplication.CreateBuilder(args);


// ================= Original Safarni Configuration =================


builder.Services.AddControllers();


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200",
                "http://127.0.0.1:4200"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();


builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
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


// AI Provider (Groq)
builder.Services.AddScoped<IAIProvider, GroqProvider>();


// Embeddings
builder.Services.AddSingleton<
    SmartComponents.LocalEmbeddings.LocalEmbedder>();

builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();


// Qdrant Vector Database
builder.Services.AddHttpClient<IQdrantService, QdrantService>();


// Indexing
builder.Services.AddScoped<IIndexingService, IndexingService>();


// Prompt Builder
builder.Services.AddScoped<IPromptBuilder, PromptBuilder>();


// Database Seeder
builder.Services.AddScoped<ChatDbSeeder>();


// Intent Detection
builder.Services.AddScoped<IIntentDetectionService, IntentDetectionService>();


// Google Places API
builder.Services.AddHttpClient<
    IGooglePlacesService,
    GooglePlacesService>();



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



app.UseStaticFiles();



app.UseCors("AllowFrontend");



app.UseHttpsRedirection();



app.UseAuthentication();

app.UseAuthorization();



app.MapControllers();




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



    // RAG Qdrant Auto Indexing

    var qdrantService = scope.ServiceProvider
        .GetRequiredService<IQdrantService>();


    var indexingService = scope.ServiceProvider
        .GetRequiredService<IIndexingService>();


    try
    {

        if (await qdrantService.IsHealthyAsync())
        {

            await qdrantService.CreateCollectionAsync();


            await indexingService.ReindexAllAsync();


            Console.WriteLine(
                "RAG indexing completed successfully.");

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

static void LoadLocalEnvironmentFile()
{
    var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

    while (directory is not null)
    {
        var envPath = Path.Combine(directory.FullName, ".env");

        if (File.Exists(envPath))
        {
            foreach (var rawLine in File.ReadAllLines(envPath))
            {
                var line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');

                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim().Trim('"');

                if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                {
                    Environment.SetEnvironmentVariable(key, value);
                }
            }

            return;
        }

        directory = directory.Parent;
    }
}
