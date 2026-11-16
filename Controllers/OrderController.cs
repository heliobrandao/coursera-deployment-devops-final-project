using LogiTrack.Data;
using LogiTrack.Dtos;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LogiTrack.Controllers;

[Authorize]
[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly LogiTrackContext _context;

    public OrderController(LogiTrackContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAll()
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .OrderByDescending(order => order.DatePlaced)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == id);

        if (order is null)
        {
            return NotFound(new
            {
                message = $"Order {id} was not found."
            });
        }

        return Ok(order);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<Order>> Create(
        CreateOrderRequest request
    )
    {
        var requestedItemIds = request.ItemIds
            .Distinct()
            .ToList();

        var items = await _context.InventoryItems
            .Where(item => requestedItemIds.Contains(item.ItemId))
            .ToListAsync();

        if (items.Count != requestedItemIds.Count)
        {
            var foundItemIds = items
                .Select(item => item.ItemId)
                .ToHashSet();

            var missingItemIds = requestedItemIds
                .Where(id => !foundItemIds.Contains(id))
                .ToList();

            return BadRequest(new
            {
                message = "One or more inventory items were not found.",
                missingItemIds
            });
        }

        var unavailableItemIds = items
            .Where(item => item.OrderId is not null)
            .Select(item => item.ItemId)
            .ToList();

        if (unavailableItemIds.Count > 0)
        {
            return Conflict(new
            {
                message =
                    "One or more inventory items already belong to an order.",
                unavailableItemIds
            });
        }

        var order = new Order
        {
            CustomerName = request.CustomerName.Trim(),
            DatePlaced = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            order.AddItem(item);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.OrderId },
            order
        );
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _context.Orders
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.OrderId == id);

        if (order is null)
        {
            return NotFound(new
            {
                message = $"Order {id} was not found."
            });
        }

        foreach (var item in order.Items)
        {
            item.OrderId = null;
            item.Order = null;
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}