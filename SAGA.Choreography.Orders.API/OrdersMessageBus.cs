using RabbitMQ.Client;
using SAGA.Choreography.Orders.API.Evenets;
using System.Text.Json;
using System.Text;

namespace SAGA.Choreography.Orders.API;

public static class OrdersMessageBus
{
    private const string ExchangeName = "order_events";
    public static async Task Publish<T>(IEvent @event, string routingKey) where T : IEvent
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

        await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct, durable: true);

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: routingKey,
            body: body
        );

        Console.WriteLine($"{routingKey} event published to RabbitMQ");
    }
}
