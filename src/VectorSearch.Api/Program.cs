using VectorSearch.Core;
using System.IO;
using System.Text.Json.Serialization;

namespace VectorSearch.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Vector Search API", Version = "v1" });
        });

        // Configure application services
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Get index path from configuration
        string indexPath = configuration["IndexPath"] ?? 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.pb");

        // Register the index loader
        services.AddSingleton(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<IndexLoader>>();
            try
            {
                logger.LogInformation("Loading vector index from {IndexPath}", indexPath);
                var index = IndexLoader.LoadIndex(indexPath);
                logger.LogInformation("Successfully loaded index with {ClusterCount} clusters", index.Clusters.Count);
                return index;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load vector index from {IndexPath}", indexPath);
                throw;
            }
        });

        // Register IVFSearchEngine
        services.AddSingleton<IVFSearchEngine>();

        // Configure embedding service based on configuration
        string embeddingProvider = configuration["EmbeddingProvider"]?.ToLowerInvariant() ?? "openai";

        if (embeddingProvider == "mock")
        {
            services.AddSingleton<IEmbeddingService, MockEmbeddingService>();
        }
        else
        {
            services.AddSingleton<IEmbeddingService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<OpenAiEmbeddingService>>();
                var apiKey = configuration["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI ApiKey is required");
                var endpoint = configuration["OpenAI:Endpoint"];
                var deploymentName = configuration["OpenAI:DeploymentName"] ?? "text-embedding-3-small";
                
                return new OpenAiEmbeddingService(apiKey, endpoint, deploymentName, logger);
            });
        }
    }
}
