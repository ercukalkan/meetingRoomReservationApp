namespace Data.DTOs;

public class RecurringReservationDTO : BaseDTO
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public int NumberOfWeeks { get; set; }
    public Guid RoomId { get; set; }
    public RoomDTO? Room { get; set; }
    public Guid UserId { get; set; }
    public UserDTO? User { get; set; }
    public List<ReservationDTO>? Reservations { get; set; }
}