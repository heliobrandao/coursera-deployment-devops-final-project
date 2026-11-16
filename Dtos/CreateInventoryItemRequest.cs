using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Dtos;

public class CreateInventoryItemRequest
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(100)]
    public string Location { get; set; } = string.Empty;
}