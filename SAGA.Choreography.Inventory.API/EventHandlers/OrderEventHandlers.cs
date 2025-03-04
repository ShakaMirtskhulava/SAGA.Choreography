using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace SAGA.Choreography.Inventory.API.EventHandlers;

public class OrderEventHandlers : BackgroundService
{
    private const string ExchangeName = "order_events";
    private readonly ConnectionFactory _factory;
    private static int s_counter = 1;

    public OrderEventHandlers()
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

        await channel.QueueBindAsync(queue: "order_created_queue", exchange: ExchangeName, routingKey: nameof(OrderCreated));

        await StartConsumer(channel, "order_created_queue", OrderCreatedEventHandler);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static async Task StartConsumer(IChannel channel, string queue, AsyncEventHandler<BasicDeliverEventArgs> handler)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += handler;
        await channel.BasicConsumeAsync(queue: queue, autoAck: true, consumer: consumer);
    }

    private static async Task OrderCreatedEventHandler(object sender, BasicDeliverEventArgs @event)
    {
        var body = @event.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        var orderCreated = JsonSerializer.Deserialize<OrderCreated>(message)!;
        Console.WriteLine($"OrderCreated event received: {orderCreated}");
        try
        {
            if (s_counter % 2 == 0)
                throw new Exception("Couldn't update the inventory");
            
            s_counter++;
            await Task.Delay(1000);
            Console.WriteLine($"Inventory updated for product {orderCreated.productId} with quantity {orderCreated.quantity}");
        }
        catch
        {
            Console.WriteLine($"Inventory update failed for product {orderCreated.productId} with quantity {orderCreated.quantity}");
            OrderCreationFailed orderCreationResult = new(orderCreated.id);
            await MessageBus.Publish<OrderCreationFailed>(orderCreationResult,ExchangeName, nameof(OrderCreationFailed));
        }
    }
}
