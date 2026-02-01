using EventWorker;
using EventWorker.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<PulseOpsDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});



builder.Services.AddScoped<IIncidentEventRepository, IncidentEventRepository>();
builder.Services.AddScoped<IEventIngestionService, EventIngestionService>();

builder.Services.AddSingleton<IRabbitMqTopology, RabbitMqTopology>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
