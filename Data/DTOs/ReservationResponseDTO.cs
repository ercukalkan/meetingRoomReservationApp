namespace Data.DTOs;

public class ReservationResponseDTO : BaseDTO
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public RoomResponseDTO? Room { get; set; }
    public UserResponseDTO? User { get; set; }
}