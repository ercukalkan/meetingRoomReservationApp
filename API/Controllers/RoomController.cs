using Data.Context;
using Data.DTOs;
using Data.Entities;
using Data.Filtering;
using Data.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Core.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResponseSchema<IReadOnlyList<Room>>>> GetRooms([FromQuery] FilterParams filterParams)
    {
        if (!filterParams.Start.HasValue)
            throw new BadRequestException("Start date is required.");

        if (!filterParams.End.HasValue)
            throw new BadRequestException("End date is required.");

        if (filterParams.Start >= filterParams.End)
            throw new BadRequestException("Start must be earlier than End.");

        var query = await _context.Rooms
            .Where(r => r.Reservations.All(res => res.End <= filterParams.Start || res.Start >= filterParams.End)) // Filter rooms that are available between the specified dates
            .Select(r => new RoomDTO
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Floor = r.Floor,
                Equipments = r.Equipments.Select(e => new EquipmentDTO { Id = e.Id, Name = e.Name }).ToList()
            })
            .ToListAsync();

        return Ok(new ResponseSchema<List<RoomDTO>>
        {
            Message = $"Rooms between {filterParams.Start} and {filterParams.End} retrieved successfully.",
            Success = true,
            Data = query
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Room>> GetRoom(Guid id)
    {
        var room = await _context.Rooms
            .Where(r => r.Id == id)
            .Select(r => new RoomDTO
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Floor = r.Floor,
                Equipments = r.Equipments.Select(e => new EquipmentDTO { Id = e.Id, Name = e.Name }).ToList()
            })
            .FirstOrDefaultAsync();

        if (room == null)
            return NotFound();

        return Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<Room>> CreateRoom(RoomDTO dto)
    {
        if (dto == null)
            return BadRequest("Room data is required.");

        var equipments = await _context.Equipments
            .Where(e => dto.Equipments != null && dto.Equipments.Select(eq => eq.Id).Contains(e.Id))
            .ToListAsync();

        var newRoom = new Room
        {
            Name = dto.Name,
            Capacity = dto.Capacity,
            Floor = dto.Floor,
            Equipments = equipments
        };

        _context.Rooms.Add(newRoom);
        await _context.SaveChangesAsync();

        var createdRoom = await _context.Rooms
            .Where(r => r.Id == newRoom.Id)
            .Select(r => new RoomDTO
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Floor = r.Floor,
                Equipments = r.Equipments.Select(e => new EquipmentDTO { Id = e.Id, Name = e.Name }).ToList()
            })
            .FirstOrDefaultAsync();

        if (createdRoom != null)
        {
            return CreatedAtAction(
                nameof(GetRoom),
                new { id = newRoom.Id },
                createdRoom
            );
        }

        return BadRequest("Wrong object structure.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRoom(Guid id, RoomDTO dto)
    {
        if (id != dto.Id)
            return BadRequest("ID mismatch.");

        var room = await _context.Rooms.Include(r => r.Equipments).FirstOrDefaultAsync(r => r.Id == id);

        if (room == null)
            return NotFound($"Room with Id {id} not found.");

        UpdateRoomProperties(room, dto);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var room = await _context.Rooms.FindAsync(id);

        if (room == null)
            return NotFound();

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("betweenDates")]
    public async Task<ActionResult<ResponseSchema<List<RoomDTO>>>> GetRoomsBetweenDates(DateTime start, DateTime end)
    {
        var rooms = await _context.Rooms
            .Where(r => r.Reservations.All(res => res.End <= start || res.Start >= end))
            .Select(r => new RoomDTO
            {
                Id = r.Id,
                Name = r.Name,
                Capacity = r.Capacity,
                Floor = r.Floor,
                Equipments = r.Equipments.Select(e => new EquipmentDTO { Id = e.Id, Name = e.Name }).ToList()
            })
            .ToListAsync();

        return Ok(new ResponseSchema<List<RoomDTO>>
        {
            Message = "Rooms retrieved successfully.",
            Success = true,
            Data = rooms
        });
    }

    private async Task<bool> RoomExists(Guid id)
    {
        return await _context.Rooms.AnyAsync(e => e.Id == id);
    }

    private void UpdateRoomProperties(Room room, RoomDTO dto)
    {
        room.Name = dto.Name;
        room.Capacity = dto.Capacity;
        room.Floor = dto.Floor;

        var idsToAdd = dto.Equipments?
            .Select(e => e.Id)
            .Except(room.Equipments.Select(e => e.Id))
            .ToList() ?? [];

        var idsToRemove = room.Equipments
            .Select(e => e.Id)
            .Except(dto.Equipments?.Select(e => e.Id) ?? []).ToList();

        var equipmentsToAdd = _context.Equipments.Where(e => idsToAdd.Contains(e.Id)).ToList();
        var equipmentsToRemove = room.Equipments.Where(e => idsToRemove.Contains(e.Id)).ToList();

        equipmentsToAdd.ForEach(room.Equipments.Add);
        equipmentsToRemove.ForEach(e => room.Equipments.Remove(e));
    }
}