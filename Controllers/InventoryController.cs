using LogiTrack.Data;
using LogiTrack.Dtos;
using LogiTrack.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace LogiTrack.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly LogiTrackContext _context;

    public InventoryController(LogiTrackContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<InventoryItem>>> GetAll()
    {
        var items = await _context.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<ActionResult<InventoryItem>> Create(
        CreateInventoryItemRequest request
    )
    {
        var item = new InventoryItem
        {
            Name = request.Name.Trim(),
            Quantity = request.Quantity,
            Location = request.Location.Trim()
        };

        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.ItemId },
            item
        );
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryItem>> GetById(int id)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ItemId == id);

        if (item is null)
        {
            return NotFound(new
            {
                message = $"Inventory item {id} was not found."
            });
        }

        return Ok(item);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.InventoryItems.FindAsync(id);

        if (item is null)
        {
            return NotFound(new
            {
                message = $"Inventory item {id} was not found."
            });
        }

        if (item.OrderId is not null)
        {
            return Conflict(new
            {
                message =
                    "The inventory item cannot be deleted because " +
                    "it belongs to an order."
            });
        }

        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}