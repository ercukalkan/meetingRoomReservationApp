namespace Data.Filtering;

public class FilterParams
{
    public DateTime? Date { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public Guid? RoomId { get; set; }
    public Guid? UserId { get; set; }
}