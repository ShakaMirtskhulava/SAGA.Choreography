using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace SAGA.Choreography.Inventory.API;

public static class MessageBus
{
    public static async Task Publish<T>(IEvent @event, string exchangeName,string routingKey) where T : IEvent
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "user",
            Password = "password"
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        var message = JsonSerializer.Serialize(@event, @event.GetType());
        ReadOnlyMemory<byte> body = Encoding.UTF8.GetBytes(message);

        await channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Direct, durable: true);

        await channel.BasicPublishAsync(
            exchange: exchangeName,
            routingKey: routingKey,
            body: body
        );

        Console.WriteLine("{EventName} event published to RabbitMQ: {EventData}", routingKey, @event);
    }
}
