namespace EventWorker.Infrastructure;

using RabbitMQ.Client;

public class RabbitMqTopology : IRabbitMqTopology
{
    public async Task SetupAsync(IChannel channel, CancellationToken cancellationToken)
    {

        await channel.ExchangeDeclareAsync(
                exchange: "events.dlx",
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken
            );



        await channel.QueueDeclareAsync(
            queue: "events.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );


        await channel.QueueBindAsync(
            queue: "events.dlq",
            exchange: "events.dlx",
            routingKey: "events.raw",
            arguments: null,
            cancellationToken: cancellationToken
        );



        await channel.ExchangeDeclareAsync(
            exchange: "events.retry.dlx",
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken
        );

        await channel.QueueDeclareAsync(
             queue: "events.raw",
             durable: true,
             exclusive: false,
             autoDelete: false,
             arguments: new Dictionary<string, object?>
             {
                 ["x-dead-letter-exchange"] = "events.dlx",
                 ["x-dead-letter-routing-key"] = "events.raw"
             },
             cancellationToken: cancellationToken
         );


        await channel.QueueBindAsync(
            queue: "events.raw",
            exchange: "events.retry.dlx",
            routingKey: "events.raw",
            arguments: null,
            cancellationToken: cancellationToken
        );



        await channel.QueueDeclareAsync(
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
            cancellationToken: cancellationToken
        );
    }
}