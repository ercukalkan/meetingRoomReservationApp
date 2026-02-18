using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class Room : BaseEntity
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(25, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 25 characters.")]
    public string Name { get; set; } = string.Empty;
    [Range(3, 10, ErrorMessage = "Capacity must be between 3 and 10")]
    public int Capacity { get; set; }
    [Range(1, 5, ErrorMessage = "Floor must be between 1 and 5")]
    public int Floor { get; set; }

    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<Equipment> Equipments { get; set; } = [];
    public ICollection<RecurringReservation> RecurringReservations { get; set; } = [];
}