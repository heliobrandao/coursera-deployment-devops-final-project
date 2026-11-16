using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LogiTrack.Models;

public class InventoryItem
{
    [Key]
    public int ItemId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;

    // Chave estrangeira opcional.
    // Um item pode existir no estoque sem estar associado a um pedido.
    public int? OrderId { get; set; }

    [JsonIgnore]
    public Order? Order { get; set; }

    public void DisplayInfo()
    {
        Console.WriteLine(
            $"Item: {Name} | Quantity: {Quantity} | Location: {Location}"
        );
    }
}