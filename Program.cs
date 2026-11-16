using LogiTrack.Data;
using LogiTrack.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<LogiTrackContext>(options =>
{
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' was not found."
        );

    options.UseSqlite(connectionString);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<LogiTrackContext>();

    await context.Database.MigrateAsync();

    if (!await context.InventoryItems.AnyAsync())
    {
        var palletJack = new InventoryItem
        {
            Name = "Pallet Jack",
            Quantity = 12,
            Location = "Warehouse A"
        };

        var forklift = new InventoryItem
        {
            Name = "Forklift",
            Quantity = 5,
            Location = "Warehouse B"
        };

        context.InventoryItems.AddRange(palletJack, forklift);
        await context.SaveChangesAsync();
    }

    var items = await context.InventoryItems
        .AsNoTracking()
        .ToListAsync();

    foreach (var item in items)
    {
        item.DisplayInfo();
    }
}

app.MapGet("/", () => Results.Ok(new
{
    application = "LogiTrack",
    status = "Running"
}));

app.MapGet(
    "/inventory",
    async (LogiTrackContext context) =>
    {
        var items = await context.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        return Results.Ok(items);
    }
);

app.MapPost(
    "/orders",
    async (Order order, LogiTrackContext context) =>
    {
        order.OrderId = 0;
        order.DatePlaced = DateTime.Now;

        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return Results.Created(
            $"/orders/{order.OrderId}",
            order
        );
    }
);

app.MapPost(
    "/orders/{orderId:int}/items/{itemId:int}",
    async (
        int orderId,
        int itemId,
        LogiTrackContext context
    ) =>
    {
        var order = await context.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == orderId);

        if (order is null)
        {
            return Results.NotFound(
                $"Order {orderId} not found."
            );
        }

        var item = await context.InventoryItems
            .FindAsync(itemId);

        if (item is null)
        {
            return Results.NotFound(
                $"Inventory item {itemId} not found."
            );
        }

        var added = order.AddItem(item);

        if (!added)
        {
            return Results.BadRequest(
                "Item could not be added to the order."
            );
        }

        await context.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Item added successfully.",
            summary = order.GetOrderSummary()
        });
    }
);

app.MapGet(
    "/orders/{orderId:int}/summary",
    async (
        int orderId,
        LogiTrackContext context
    ) =>
    {
        var order = await context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == orderId);

        if (order is null)
        {
            return Results.NotFound(
                $"Order {orderId} not found."
            );
        }

        return Results.Ok(new
        {
            summary = order.GetOrderSummary()
        });
    }
);

app.MapDelete(
    "/orders/{orderId:int}/items/{itemId:int}",
    async (
        int orderId,
        int itemId,
        LogiTrackContext context
    ) =>
    {
        var order = await context.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == orderId);

        if (order is null)
        {
            return Results.NotFound(
                $"Order {orderId} not found."
            );
        }

        var removed = order.RemoveItem(itemId);

        if (!removed)
        {
            return Results.NotFound(
                $"Item {itemId} was not found in order {orderId}."
            );
        }

        await context.SaveChangesAsync();

        return Results.Ok(new
        {
            message = "Item removed successfully.",
            summary = order.GetOrderSummary()
        });
    }
);

app.Run();