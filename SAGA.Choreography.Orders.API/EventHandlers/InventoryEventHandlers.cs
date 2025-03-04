using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SAGA.Choreography.Orders.API.Evenets;
using System.Text;
using System.Text.Json;

namespace SAGA.Choreography.Orders.API.EventHandlers;

public class InventoryEventHandlers : BackgroundService
{
    private const string ExchangeName = "order_events";
    private readonly ConnectionFactory _factory;

    public InventoryEventHandlers()
    {
        _factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "user",
            Password = "password"
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var connection = await _factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct, durable: true);

        await channel.QueueDeclareAsync(queue: "order_created_queue", durable: true, exclusive: false, autoDelete: false);

        await channel.QueueBindAsync(queue: "order_created_queue", exchange: ExchangeName, routingKey: nameof(OrderCreationFailed));

        await StartConsumer(channel, "order_created_queue", OrderCreationFailedEventHandler);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task StartConsumer(IChannel channel, string queue, AsyncEventHandler<BasicDeliverEventArgs> handler)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += handler;
        await channel.BasicConsumeAsync(queue: queue, autoAck: true, consumer: consumer);
    }

    private static async Task OrderCreationFailedEventHandler(object sender, BasicDeliverEventArgs @event)
    {
        var body = @event.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var orderCreationFailed = JsonSerializer.Deserialize<OrderCreationFailed>(message)!;
        Console.WriteLine($"OrderCreationFailed event received: {orderCreationFailed}");

        Console.WriteLine($"Doing compensating transaction for order with id: {orderCreationFailed.orderId}");
        await Task.Delay(1000);
    }
}
