using BuildingBlocks.Behaviors;
using BuildingBlocks.Exceptions.Handler;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Orders.API.Data;
using Orders.API.Services;

// Licencia Community de QuestPDF (gratuita para individuos/empresas pequeñas). Requerida desde
// QuestPDF 2023+ o la librería lanza excepción al generar el primer documento.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Application services
builder.Services.AddCarter();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy
            .WithOrigins("https://mellow-bonbon-0b2969.netlify.app", "http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.AddMediatR(config =>
{
    config.RegisterServicesFromAssembly(typeof(Program).Assembly);
    config.AddOpenBehavior(typeof(ValidationBehavior<,>));
    config.AddOpenBehavior(typeof(LoggingBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Data services (MongoDB Atlas) — la cadena de conexión SOLO viene de la variable de entorno
// ConnectionStrings__MongoDb, nunca de appsettings.json (ver appsettings.json de este proyecto).
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
builder.Services.PostConfigure<MongoDbSettings>(settings =>
{
    settings.ConnectionString = builder.Configuration.GetConnectionString("MongoDb")
        ?? throw new InvalidOperationException(
            "Falta la variable de entorno ConnectionStrings__MongoDb con la cadena de conexión de MongoDB Atlas.");
});

// MongoClient se registra como singleton (recomendación oficial del driver): tanto el
// repositorio como el health check de Mongo lo resuelven del contenedor.
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});
builder.Services.AddSingleton<IOrderRepository, OrderRepository>();
builder.Services.AddSingleton<IOrderPdfGenerator, OrderPdfGenerator>();

// Cliente HTTP hacia Basket.API — BaseAddress viene de Services:BasketApi (env var Services__BasketApi).
builder.Services.AddHttpClient<IBasketApiClient, BasketApiClient>(client =>
{
    var basketApiUrl = builder.Configuration["Services:BasketApi"]
        ?? throw new InvalidOperationException("Falta la configuración 'Services:BasketApi' (URL de Basket.API).");
    client.BaseAddress = new Uri(basketApiUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Cross-cutting
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddMongoDb(name: "mongodb");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Orders.API",
        Version = "v1",
        Description = "Genera y consulta órdenes de compra a partir del Basket. Persistencia exclusiva en MongoDB Atlas."
    });
    options.AddSecurityDefinition("Idempotency-Key", new OpenApiSecurityScheme
    {
        Name = "Idempotency-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Header requerido en POST /api/orders para garantizar idempotencia."
    });
});

var app = builder.Build();

// Pipeline
app.UseRouting();
app.UseCors("AllowFrontend");
app.MapCarter();
app.UseExceptionHandler(options => { });

app.UseSwagger();
app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders.API v1"));

app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
