using K4os.WebApp.Bootstrap;
using WebApp.Host;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.ConfigureMetrics();
builder.ConfigureTracing();
builder.ConfigureHealthChecks();

builder.Services.AddHostedService<BackgroundLoop>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHealthChecks("/health");

app.MapGet("/", () => "All good here").WithName("root").WithOpenApi();

app.Run();
