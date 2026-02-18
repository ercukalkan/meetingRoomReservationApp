using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Data.Entities;

public class Reservation : BaseEntity
{
    [Required(ErrorMessage = "Start date is required.")]
    [Compare("End", ErrorMessage = "Start date must be before end date.")]
    public DateTime Start { get; set; }
    [Required(ErrorMessage = "End date is required.")]
    public DateTime End { get; set; }

    [Required(ErrorMessage = "Room is required.")]
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    [Required(ErrorMessage = "User is required.")]
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;


    public Guid? RecurringReservationId { get; set; }
    public RecurringReservation? RecurringReservation { get; set; }
}