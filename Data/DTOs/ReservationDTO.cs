namespace Data.DTOs;

public class ReservationDTO : BaseDTO
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Guid RoomId { get; set; }
    public RoomDTO? Room { get; set; }
    public Guid UserId { get; set; }
    public UserDTO? User { get; set; }
    public Guid? RecurringReservationId { get; set; }
}