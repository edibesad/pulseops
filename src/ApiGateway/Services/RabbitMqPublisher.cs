using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class RabbitMqPublisher : IEventPublisher, IAsyncDisposable
{

    private readonly IConnection _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IConnection connection)
    {
        _connection = connection;

    }

    public async Task PublishAsync<T>(T message)
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

        _channel ??= await _connection.CreateChannelAsync();

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: "events.raw",
            mandatory: false,
            basicProperties: props,
            body: body
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel != null)
        {
            await _channel.CloseAsync();
        }
        await _connection.CloseAsync();
    }


}