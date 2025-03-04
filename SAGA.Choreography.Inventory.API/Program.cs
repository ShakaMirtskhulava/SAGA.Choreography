using SAGA.Choreography.Inventory.API.EventHandlers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<OrderEventHandlers>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


await app.RunAsync();


public record Invenotry(int productId, int quantity);
public interface IEvent;
public record OrderCreated(int id, int productId, int quantity, DateTime timeStamp) : IEvent;
public record OrderCreationFailed(int orderId) : IEvent;
