namespace EventWorker.Infrastructure;

using RabbitMQ.Client;

public interface IRabbitMqTopology
{
    Task SetupAsync(IChannel channel, CancellationToken cancellationToken);
}