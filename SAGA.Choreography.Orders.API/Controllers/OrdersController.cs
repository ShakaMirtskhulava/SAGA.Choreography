using Microsoft.AspNetCore.Mvc;
using SAGA.Choreography.Orders.API.Evenets;
using SAGA.Choreography.Orders.API.Models;

namespace SAGA.Choreography.Orders.API.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public async Task<Order> Create([FromBody] Order order)
    {
        _logger.LogInformation("Order was created and written in the database: {@order}", order);

        OrderCreated orderCreated = new(order.id, order.productId, order.quantity);
        await OrdersMessageBus.Publish<OrderCreated>(orderCreated, nameof(OrderCreated));

        return order;
    }

}
