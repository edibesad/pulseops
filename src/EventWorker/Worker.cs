using EventWorker.Application;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace EventWorker
{
    public class Worker(IConfiguration config, IServiceScopeFactory scopeFactory, ILogger<Worker> logger) : BackgroundService
    {
        private readonly IConfiguration _config = config;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<Worker> _logger = logger;

        private IConnection? _connection;
        private IChannel? _channel;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await StartRabbitMqAsync(stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task StartRabbitMqAsync(CancellationToken ct)
        {
            var host = _config["RABBITMQ_HOST"] ?? "localhost";
            var port = int.TryParse(_config["RABBITMQ_PORT"], out var p) ? p : 5672;
            var user = _config["RABBITMQ_USER"] ?? "";
            var pass = _config["RABBITMQ_PASS"] ?? "";


            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = pass,
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct);




            await _channel.ExchangeDeclareAsync(
                exchange: "events.dlx",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct
            );



            await _channel.QueueDeclareAsync(
                queue: "events.dlq",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct
            );


            await _channel.QueueBindAsync(
                queue: "events.dlq",
                exchange: "events.dlx",
                routingKey: "events.raw",
                arguments: null,
                cancellationToken: ct
            );



            await _channel.ExchangeDeclareAsync(
                exchange: "events.retry.dlx",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct
            );

            await _channel.QueueDeclareAsync(
                 queue: "events.raw",
                 durable: true,
                 exclusive: false,
                 autoDelete: false,
                 arguments: new Dictionary<string, object?>
                 {
                     ["x-dead-letter-exchange"] = "events.dlx",
                     ["x-dead-letter-routing-key"] = "events.raw"
                 },
                 cancellationToken: ct
             );


            await _channel.QueueBindAsync(
                queue: "events.raw",
                exchange: "events.retry.dlx",
                routingKey: "events.raw",
                arguments: null,
                cancellationToken: ct
            );



            await _channel.QueueDeclareAsync(
                queue: "events.retry.5s",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = 5000, // 5 saniye beklet
                    ["x-dead-letter-exchange"] = "events.retry.dlx",
                    ["x-dead-letter-routing-key"] = "events.raw"
                },
                cancellationToken: ct
            );


            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.ReceivedAsync += async (_, ea) =>
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();

                    var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                    var ingestion = scope.ServiceProvider.GetRequiredService<IEventIngestionService>();

                    var eventType = GetHeaderString(ea, "type") ?? "Unknown";

                    await ingestion.IngestRawEventAsync(eventType, json, ct);

                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                }
                catch (DomainValidationException ex)
                {
                    _logger.LogWarning(ex, "Validation failed -> DLQ. DeliveryTag={DeliveryTag}", ea.DeliveryTag);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
                }
                catch (Exception ex)
                {
                    var retryCount = GetRetryCount(ea);

                    if (IsTransient(ex) && retryCount < 3)
                    {
                        var nextRetry = retryCount + 1;

                        var props = new BasicProperties
                        {
                            ContentType = ea.BasicProperties?.ContentType ?? "application/json",
                            DeliveryMode = DeliveryModes.Persistent,
                            Headers = ea.BasicProperties?.ContentType is null ? new Dictionary<string, object?>()
                            : new Dictionary<string, object?>(ea.BasicProperties.Headers!)

                        };

                        props.Headers["x-retry-count"] = nextRetry.ToString();

                        if (!props.Headers.ContainsKey("type") && ea.BasicProperties?.Headers?.TryGetValue("type", out var t) == true) props.Headers["type"] = t;

                        await _channel.BasicPublishAsync(
                            exchange: "",
                            routingKey: "events.retry.5s",
                            mandatory: false,
                            basicProperties: props,
                            body: ea.Body,
                            cancellationToken: ct
                        );

                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);

                        _logger.LogWarning(ex, "Transient error -> RETRY {Retry}/3 after 5s", nextRetry);
                        return;
                    }
                    _logger.LogError(ex, "Unhandled or max retry reached -> DLQ. Retry={Retry}", retryCount);
                    await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: ct);
                }
            };

            await _channel.BasicConsumeAsync(
                "events.raw",
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct
            );

            _logger.LogInformation("EventWorker consuming queue: events.raw");

        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_channel is not null) await _channel.CloseAsync(cancellationToken);
                if (_connection is not null) await _connection.CloseAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "");
            }

            await base.StopAsync(cancellationToken);
        }

        private string? GetHeaderString(BasicDeliverEventArgs ea, string key)
        {
            if (ea.BasicProperties?.Headers is null) return null;
            if (!ea.BasicProperties.Headers.TryGetValue(key, out var value)) return null;

            return value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                ReadOnlyMemory<byte> rom => Encoding.UTF8.GetString(rom.ToArray()),
                string s => s,
                _ => null
            };
        }

        private static int GetRetryCount(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties?.Headers is null) return 0;
            if (!ea.BasicProperties.Headers.TryGetValue("x-retry-count", out var v)) return 0;

            return v switch
            {
                byte[] b when int.TryParse(Encoding.UTF8.GetString(b), out var n) => n,
                int n => n,
                long l => (int)l,
                _ => 0
            };
        }

        private static bool IsTransient(Exception ex)
        {
            return ex is TimeoutException
                || ex.GetType().Name.Contains("Npgsql", StringComparison.OrdinalIgnoreCase)
                || ex.GetType().Name.Contains("DbUpdate", StringComparison.OrdinalIgnoreCase)
                || ex.Message.Contains("transient")
                ;
        }

    }

}
