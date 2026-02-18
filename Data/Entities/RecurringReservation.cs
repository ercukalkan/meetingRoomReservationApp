using System.ComponentModel.DataAnnotations;

namespace Data.Entities;

public class RecurringReservation : BaseEntity
{
    [Required(ErrorMessage = "Start date is required.")]
    public DateTime Start { get; set; }
    [Required(ErrorMessage = "End date is required.")]
    public DateTime End { get; set; }

    [Required(ErrorMessage = "Room is required.")]
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [Required(ErrorMessage = "User is required.")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ICollection<Reservation> Reservations { get; set; } = [];
}