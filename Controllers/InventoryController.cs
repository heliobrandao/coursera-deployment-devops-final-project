using System.Diagnostics;
using LogiTrack.Data;
using LogiTrack.Dtos;
using LogiTrack.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace LogiTrack.Controllers;

[Authorize]
[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    private readonly LogiTrackContext _context;
    private readonly IMemoryCache _cache;

    private const string InventoryCacheKey = "inventory-list";

    public InventoryController(
        LogiTrackContext context,
        IMemoryCache cache
    )
    {
        _context = context;
        _cache = cache;
    }

    // ======================================================
    // GET - LISTAR INVENTÁRIO
    // ======================================================

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var stopwatch = Stopwatch.StartNew();

        if (_cache.TryGetValue(
                InventoryCacheKey,
                out List<InventoryItem>? cachedItems
            ))
        {
            stopwatch.Stop();

            return Ok(new
            {
                source = "Cache",
                elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
                items = cachedItems
            });
        }

        var items = await _context.InventoryItems
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .ToListAsync();

        var cacheOptions =
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(
                    TimeSpan.FromSeconds(30)
                );

        _cache.Set(
            InventoryCacheKey,
            items,
            cacheOptions
        );

        stopwatch.Stop();

        return Ok(new
        {
            source = "Database",
            elapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds,
            items
        });
    }

    // ======================================================
    // GET - ITEM POR ID
    // ======================================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.ItemId == id
            );

        if (item is null)
        {
            return NotFound(new
            {
                message =
                    $"Inventory item {id} was not found."
            });
        }

        return Ok(item);
    }

    // ======================================================
    // POST - CRIAR ITEM
    // ======================================================

    [HttpPost]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Create(
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

        // O inventário mudou.
        // Remove o cache antigo.
        _cache.Remove(InventoryCacheKey);

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.ItemId },
            item
        );
    }

    // ======================================================
    // DELETE - REMOVER ITEM
    // ======================================================

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var item =
            await _context.InventoryItems.FindAsync(id);

        if (item is null)
        {
            return NotFound(new
            {
                message =
                    $"Inventory item {id} was not found."
            });
        }

        if (item.OrderId is not null)
        {
            return Conflict(new
            {
                message =
                    "The inventory item cannot be deleted " +
                    "because it belongs to an order."
            });
        }

        _context.InventoryItems.Remove(item);

        await _context.SaveChangesAsync();

        // O inventário mudou.
        // Remove o cache antigo.
        _cache.Remove(InventoryCacheKey);

        return NoContent();
    }
}