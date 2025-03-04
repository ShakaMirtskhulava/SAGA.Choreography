namespace SAGA.Choreography.Orders.API.Evenets;

public record OrderCreated(int id, int productId, int quantity, DateTime timeStamp) : IEvent
{
    public OrderCreated(
        int id,
        int productId,
        int quantity
    ) : this(id, productId, quantity, DateTime.UtcNow)
    {
    }
}

public record OrderCreationFailed(int orderId);