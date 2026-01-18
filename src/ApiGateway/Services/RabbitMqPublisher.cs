using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{

    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqPublisher(IConfiguration config)
    {
        var host = config["RabbitMq:Host"];
        var user = config["RabbitMq:User"];
        var pass = config["RabbitMq:Password"];
        var port = int.Parse(config["RabbitMq:Port"]!);


        var factory = new ConnectionFactory
        {
            HostName = host!,
            Port = port,
            UserName = user!,
            Password = pass!
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

    }

    public void Publish<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Headers = new Dictionary<string, object?>
            {
                ["type"] = typeof(T).Name
            }
        };

        _channel.BasicPublishAsync(
            exchange: "",
            routingKey: "events.raw",
            mandatory: false,
            basicProperties: props,
            body: body
        ).GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}