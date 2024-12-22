using Microsoft.OpenApi.Models;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container
        ConfigureServices(builder.Services, builder.Configuration);

        var app = builder.Build();

        // Check for the --webapi argument to decide mode
        if (args.Contains("--webapi"))
        {
            // Set explicit URLs
            app.Urls.Add("http://localhost:5000");
            app.Urls.Add("https://localhost:5001");

            ConfigureWebApi(app);

            Console.WriteLine("Web API started.");
            Console.WriteLine("Access Swagger UI at: http://localhost:5000/swagger");
            Console.WriteLine("Or secure endpoint at: https://localhost:5001/swagger");

            await app.RunAsync();
        }
        else
        {
            // Run as console application
            using (var scope = app.Services.CreateScope())
            {
                // Check for an argument or configuration to decide which processor to run
                var runGlossaryProcessor = args.Contains("--glossary");

                if (runGlossaryProcessor)
                {
                    var glossaryProcessor = scope.ServiceProvider.GetRequiredService<GlossaryProcessorService>();
                    Console.WriteLine("Starting glossary processing...");
                    await glossaryProcessor.ProcessAllClauses();
                    Console.WriteLine("Glossary processing completed.");
                }
                else
                {
                    var taxProcessor = scope.ServiceProvider.GetRequiredService<TaxProcessorService>();
                    Console.WriteLine("Starting tax clause processing...");
                    await taxProcessor.ProcessAllClauses();
                    Console.WriteLine("Tax clause processing completed.");
                }
            }
        }
    }


    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Common Services
        services.Configure<AiSettings>(configuration.GetSection("AiSettings"));
        services.AddSingleton<HandbookDocumentService>();
        services.AddSingleton<PromptLoaderService>();
        services.AddSingleton<TaxProcessorService>();
        services.AddSingleton<GlossaryProcessorService>();
        services.AddSingleton<GlossaryDocumentService>();


        // HTTP Client factory for better HttpClient management
        services.AddHttpClient();

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
        });

        // Web API Services
        services.AddControllers();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Tax Ordinance Processing API",
                Version = "v1"
            });
        });

        // Configure AI Service based on settings
        var aiSettings = configuration.GetSection("AiSettings").Get<AiSettings>();
        if (aiSettings?.ModelProvider == "Anthropic")
        {
            services.AddSingleton<IGenAICaller>(sp =>
                new AnthropicCaller(aiSettings.AnthropicApiKey, aiSettings.HaikuModel));
        }
        else if (aiSettings?.ModelProvider == "OpenAI")
        {
            services.AddSingleton<IGenAICaller>(sp =>
                new OpenAICaller(aiSettings.OpenAiApiKey));
        }
        else
        {
            throw new InvalidOperationException($"Unsupported MODEL_PROVIDER: {aiSettings?.ModelProvider}");
        }
    }

    private static void ConfigureWebApi(WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseCors("AllowAll");
        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
    }
}