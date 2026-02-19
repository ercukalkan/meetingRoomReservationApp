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
            .Select(r => new ReservationResponseDTO
            {
                Id = r.Id,
                Start = r.Start,
                End = r.End,
                Room = new() { Id = r.RoomId, Name = r.Room.Name },
                User = new() { Id = r.UserId, Name = r.User.Name }
            })
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Reservation>> GetReservation(Guid id)
    {
        var reservation = await _context.Reservations
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

    [HttpPost]
    public async Task<ActionResult<Reservation>> CreateReservation(ReservationDTO dto)
    {
        if (dto == null)
            return BadRequest("Reservation data is required.");

        var newReservation = new Reservation
        {
            Start = dto.Start,
            End = dto.End,
            RoomId = dto.RoomId,
            UserId = dto.UserId,
            RecurringReservationId = dto.RecurringReservationId
        };

        _context.Reservations.Add(newReservation);
        await _context.SaveChangesAsync();

        var createdReservation = await _context.Reservations
            .Where(r => r.Id == newReservation.Id)
            .Select(r => new ReservationResponseDTO
            {
                Id = r.Id,
                Start = r.Start,
                End = r.End,
                Room = new() { Id = r.RoomId, Name = r.Room.Name },
                User = new() { Id = r.UserId, Name = r.User.Name }
            })
            .FirstOrDefaultAsync();

        if (createdReservation != null)
        {
            return CreatedAtAction(
               nameof(GetReservation),
               new { id = newReservation.Id },
               createdReservation
           );
        }

        return BadRequest("Wrong object structure.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReservation(Guid id, ReservationDTO dto)
    {
        if (id != dto.Id)
            return BadRequest("ID mismatch.");

        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
            return NotFound($"Reservation with Id {id} not found.");

        reservation.Start = dto.Start;
        reservation.End = dto.End;
        reservation.RoomId = dto.RoomId;
        reservation.UserId = dto.UserId;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservation(Guid id)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation == null)
            return NotFound();

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
