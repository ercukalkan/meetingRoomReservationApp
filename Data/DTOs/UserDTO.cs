namespace Data.DTOs;

public class UserDTO : BaseDTO
{
    public string? Name { get; set; } = null!;
    public List<ReservationDTO>? Reservations { get; set; }
    public List<RecurringReservationDTO>? RecurringReservations { get; set; }
}