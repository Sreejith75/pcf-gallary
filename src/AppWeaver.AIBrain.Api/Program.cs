using AppWeaver.AIBrain.Api.Services;
using AppWeaver.AIBrain;
using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "AppWeaver AI Brain API",
        Description = "API for creating PCF components using AI pipeline."
    });

    // innovative: Set the comments path for the Swagger JSON and UI.
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

// Configure Brain Path
// In development, it's relative to the running assembly (bin/Debug/...) -> project root -> solution root -> ai-brain
// We'll traverse up to find it or use config.
var brainPath = builder.Configuration["BrainPath"];
if (string.IsNullOrEmpty(brainPath))
{
    // Default relative path for dev environment
    // Api/bin/Debug/net8.0/ --> ../../../../../ai-brain
    brainPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../ai-brain"));
}

if (!Directory.Exists(brainPath))
{
    // Fallback or throw? 
    // If running from src/AppWeaver.AIBrain.Api, then ../../ai-brain might be correct.
    // Let's try to be smart or fail fast.
    Console.WriteLine($"WARNING: Brain path not found at {brainPath}. Checking alternative...");
    var altPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../../ai-brain"));
    if (Directory.Exists(altPath))
    {
        brainPath = altPath;
    }
}
Console.WriteLine($"Using Brain Path: {brainPath}");

// Register AppWeaver pipeline
builder.Services.AddAppWeaverAIBrain(brainPath, options =>
{
    options.DefaultNamespace = "Contoso";
    options.EnableCaching = true; // Enable caching for API
});

// Register Application Services
// Register Application Services
builder.Services.AddSingleton<ComponentBuildService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:3000") // Frontend URL
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
