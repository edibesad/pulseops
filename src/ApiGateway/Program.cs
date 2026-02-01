using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

var rabbitConfig = builder.Configuration.GetSection("RabbitMq");
var factory = new ConnectionFactory
{
    HostName = rabbitConfig["Host"] ?? "localhost",
    UserName = rabbitConfig["User"] ?? "guest",
    Password = rabbitConfig["Password"] ?? "guest",
    Port = int.TryParse(rabbitConfig["Port"], out var port) ? port : 5672,
};

var connection = await factory.CreateConnectionAsync();
builder.Services.AddSingleton(connection);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

builder.Configuration.AddEnvironmentVariables();





app.Run();
