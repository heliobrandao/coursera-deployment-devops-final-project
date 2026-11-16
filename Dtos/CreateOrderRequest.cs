using System.ComponentModel.DataAnnotations;

namespace LogiTrack.Dtos;

public class CreateOrderRequest
{
    [Required]
    [MaxLength(150)]
    public string CustomerName { get; set; } = string.Empty;

    public List<int> ItemIds { get; set; } = new();
}