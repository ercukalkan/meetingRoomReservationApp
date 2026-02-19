using Data.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.DTOs;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecurringReservationController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecurringReservationDTO>>> GetRecurringReservations()
    {
        var recurringReservations = await _context.RecurringReservations
            .Include(rr => rr.Room)
            .Include(rr => rr.User)
            .Select(rr => new RecurringReservationResponseDTO
            {
                Id = rr.Id,
                Start = rr.Start,
                End = rr.End,
                Room = new() { Id = rr.RoomId, Name = rr.Room.Name },
                User = new() { Id = rr.UserId, Name = rr.User.Name }
            })
            .ToListAsync();

        return Ok(recurringReservations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RecurringReservationDTO>> GetRecurringReservation(Guid id)
    {
        var recurringReservation = await _context.RecurringReservations
            .Include(rr => rr.Room)
            .Include(rr => rr.User)
            .Select(rr => new RecurringReservationResponseDTO
            {
                Id = rr.Id,
                Start = rr.Start,
                End = rr.End,
                Room = new() { Id = rr.RoomId, Name = rr.Room.Name },
                User = new() { Id = rr.UserId, Name = rr.User.Name }
            })
            .FirstOrDefaultAsync(r => r.Id == id);

        if (recurringReservation == null)
            return NotFound("Recurring Reservation not found.");

        return Ok(recurringReservation);
    }

    [HttpPost]
    public async Task<ActionResult<RecurringReservationDTO>> CreateRecurringReservation(RecurringReservationDTO dto)
    {
        if (dto == null)
            return BadRequest("Recurring Reservation data is required.");

        var newRecurringReservation = new RecurringReservation
        {
            Start = dto.Start,
            End = dto.End,
            NumberOfWeeks = dto.NumberOfWeeks,
            RoomId = dto.RoomId,
            UserId = dto.UserId
        };

        _context.RecurringReservations.Add(newRecurringReservation);
        await _context.SaveChangesAsync();

        var createdRecurringReservation = await _context.RecurringReservations
            .Include(rr => rr.Room)
            .Include(rr => rr.User)
            .Where(rr => rr.Id == newRecurringReservation.Id)
            .Select(rr => new RecurringReservationResponseDTO
            {
                Id = rr.Id,
                Start = rr.Start,
                End = rr.End,
                Room = new() { Id = rr.RoomId, Name = rr.Room.Name },
                User = new() { Id = rr.UserId, Name = rr.User.Name }
            })
            .FirstOrDefaultAsync();

        if (createdRecurringReservation != null)
        {
            _context.Reservations.AddRange(CreateCorrespondingReservations(newRecurringReservation));

            await _context.SaveChangesAsync();

            return CreatedAtAction(
               nameof(GetRecurringReservation),
               new { id = newRecurringReservation.Id },
               createdRecurringReservation
           );
        }

        return BadRequest("Wrong object structure.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecurringReservation(Guid id, RecurringReservationDTO dto)
    {
        if (id != dto.Id)
            return BadRequest("ID mismatch.");

        var recurringReservation = await _context.RecurringReservations
            .Include(rr => rr.Room)
            .Include(rr => rr.User)
            .FirstOrDefaultAsync(rr => rr.Id == id);

        if (recurringReservation == null)
            return NotFound($"Recurring Reservation with Id {id} not found.");

        recurringReservation.Start = dto.Start;
        recurringReservation.End = dto.End;
        recurringReservation.NumberOfWeeks = dto.NumberOfWeeks;
        recurringReservation.RoomId = dto.RoomId;
        recurringReservation.UserId = dto.UserId;

        _context.Reservations.RemoveRange(ClearCorrespondingReservations(id));
        _context.Reservations.AddRange(CreateCorrespondingReservations(recurringReservation));

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecurringReservation(Guid id)
    {
        var recurringReservation = await _context.RecurringReservations.FindAsync(id);

        if (recurringReservation == null)
            return NotFound();

        _context.RecurringReservations.Remove(recurringReservation);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Returns a list to create Reservation entities based on the created Recurring Reservation entity
    private List<Reservation> CreateCorrespondingReservations(RecurringReservation recurringReservation)
    {
        var list = new List<Reservation>(recurringReservation.NumberOfWeeks);

        for (int i = 0; i < recurringReservation.NumberOfWeeks; i++)
        {
            list.Add(new Reservation
            {
                Start = recurringReservation.Start.AddDays(i * 7),
                End = recurringReservation.End.AddDays(i * 7),
                RoomId = recurringReservation.RoomId,
                UserId = recurringReservation.UserId,
                RecurringReservationId = recurringReservation.Id
            });
        }

        return list;
    }

    // Clears existing Reservation entities created based on Recurring Reservation before update
    private List<Reservation> ClearCorrespondingReservations(Guid id)
    {
        return _context.Reservations.Where(r => r.RecurringReservationId == id).ToList();
    }
}