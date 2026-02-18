using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Equipment : BaseEntity
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(25, MinimumLength = 4, ErrorMessage = "Name must be between 4 and 25 characters.")]
    public string Name { get; set; } = string.Empty;

    public ICollection<Room> Rooms { get; set; } = [];
}