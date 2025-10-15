using ApiHosterLink;
using Microsoft.AspNetCore.Diagnostics;
using MongoDB.Driver;
using System.Net;
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();


// Configurar MongoDB
builder.Services.Configure<MongoDBSettings>(
    builder.Configuration.GetSection("MongoDBSettings"));

builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("MongoDBSettings").Get<MongoDBSettings>();
    var client = new MongoClient(settings.ConnectionString);
    return client.GetDatabase(settings.DatabaseName);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            error = exception?.Message,
            stackTrace = exception?.StackTrace
        });

        await context.Response.WriteAsync(result);
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();