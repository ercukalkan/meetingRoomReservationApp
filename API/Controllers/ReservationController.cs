using Data.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.DTOs;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Reservation>>> GetReservations()
    {
        var reservations = await _context.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .Select(r => new ReservationResponseDTO
            {
                Id = r.Id,
                Start = r.Start,
                End = r.End,
                Room = new() { Id = r.RoomId, Name = r.Room.Name },
                User = new() { Id = r.UserId, Name = r.User.Name }
            })
            .ToListAsync();

        return Ok(new { reservations, message = "Load successful." });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Reservation>> GetReservation(Guid id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Room)
            .Include(r => r.User)
            .Select(r => new ReservationResponseDTO
            {
                Id = r.Id,
                Start = r.Start,
                End = r.End,
                Room = new() { Id = r.RoomId, Name = r.Room.Name },
                User = new() { Id = r.UserId, Name = r.User.Name }
            })
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound("Reservation not found.");

        return Ok(reservation);
    }
}
