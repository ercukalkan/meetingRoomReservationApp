namespace Data.DTOs;

public class RoomDTO : BaseDTO
{
    public string Name { get; set; } = null!;
    public int Capacity { get; set; }
    public int Floor { get; set; }
    public List<EquipmentDTO>? Equipments { get; set; }
}