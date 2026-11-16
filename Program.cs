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

builder.Services.AddControllers();
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

app.MapControllers();

app.Run();