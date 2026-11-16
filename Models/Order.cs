using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Models;

public class Order
{
    [Key]
    public int OrderId { get; set; }

    [Required]
    [MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    public DateTime DatePlaced { get; set; } = DateTime.UtcNow;

    public List<InventoryItem> Items { get; set; } = [];

    public bool AddItem(InventoryItem item)
    {
        if (item is null)
        {
            return false;
        }

        bool itemAlreadyExists = Items.Any(
            existingItem => existingItem.ItemId != 0 &&
                            existingItem.ItemId == item.ItemId
        );

        if (itemAlreadyExists)
        {
            return false;
        }

        Items.Add(item);
        return true;
    }

    public bool RemoveItem(int itemId)
    {
        InventoryItem? item = Items.FirstOrDefault(
            inventoryItem => inventoryItem.ItemId == itemId
        );

        if (item is null)
        {
            return false;
        }

        Items.Remove(item);
        return true;
    }

    public string GetOrderSummary()
    {
        return $"Order #{OrderId} for {CustomerName} | " +
               $"Items: {Items.Count} | " +
               $"Placed: {DatePlaced:M/d/yyyy}";
    }
}