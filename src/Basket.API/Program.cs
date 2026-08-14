using Basket.API.Data;
using Basket.API.Models;
using BuildingBlocks.Behaviors;
using BuildingBlocks.Exceptions.Handler;
using FluentValidation;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Application services
builder.Services.AddCarter();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
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

// Sin esto, StoreBasketCommandValidator/DeleteBasketCommandValidator nunca se registran en el
// contenedor: ValidationBehavior recibiría siempre una lista vacía de IValidator<T> y la
// validación jamás se ejecutaría.
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Data services
builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("Database")!);
    opts.Schema.For<ShoppingCart>().Identity(x => x.UserName);
    opts.Schema.For<Customer>().Identity(x => x.Name);
}).UseLightweightSessions();

builder.Services.AddScoped<IBasketRepository, BasketRepository>();
// Si ya tienes CachedBasketRepository, descomenta:
builder.Services.Decorate<IBasketRepository, CacheBasketRepository>();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Cross-cutting
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

// Pipeline
app.UseRouting();
app.UseCors("AllowFrontend");
app.MapCarter();
app.UseExceptionHandler(options => { });

app.UseHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();