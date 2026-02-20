using Data.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.DTOs;
using Data.Filtering;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReservationResponseDTO>>> GetReservations([FromQuery] FilterParams filterParams)
    {
        var errors = new List<string>();

        if (filterParams.Start.HasValue && filterParams.End.HasValue && filterParams.Start >= filterParams.End)
        {
            errors.Add("Start must be earlier than End.");
            return BadRequest("Start must be earlier than End.");
        }

        var query = _context.Reservations.AsNoTracking();

        if (filterParams.RoomId.HasValue)
            query = query.Where(r => r.RoomId == filterParams.RoomId.Value);

        if (filterParams.UserId.HasValue)
            query = query.Where(r => r.UserId == filterParams.UserId.Value);

        if (filterParams.Start.HasValue && filterParams.End.HasValue)
            query = query.Where(r => r.Start < filterParams.End.Value && r.End > filterParams.Start.Value);

        var reservations = await query
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
    public async Task<ActionResult<ReservationResponseDTO>> GetReservation(Guid id)
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
    public async Task<ActionResult<ReservationResponseDTO>> CreateReservation(ReservationDTO dto)
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

        // Check for overlapping reservations for the same room
        if (OverlappingReservationsInRoom(_context, newReservation))
            return BadRequest("The reservation overlaps with an existing reservation for the same room.");

        // Check for maximum duration
        if (ReservationExceedsMaxDuration(newReservation))
            return BadRequest("The reservation exceeds the maximum allowed duration of 2 hours.");

        // Check if reservation is too early
        if (TooEarlyToMakeReservation(newReservation))
            return BadRequest("Cannot create a reservation that starts in more than a week from now.");

        // Check if reservation is past
        if (IsReservationPast(newReservation))
            return BadRequest("Cannot create a reservation from the past.");

        // Check if maximum reservations per user exceeded
        if (MaximumReservationsPerUserExceeded(_context, newReservation))
            return BadRequest("User cannot have more than 3 active reservations on the same day.");

        // Check if user already has a reservation that overlaps with the new reservation
        if (UserAlreadyHasReservation(_context, newReservation))
            return BadRequest("User already has a reservation that overlaps with the new reservation.");

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

        // Check for overlapping reservations for the same room
        if (OverlappingReservationsInRoom(_context, reservation))
            return BadRequest("The reservation overlaps with an existing reservation for the same room.");

        // Check for maximum duration
        if (ReservationExceedsMaxDuration(reservation))
            return BadRequest("The reservation exceeds the maximum allowed duration of 2 hours.");

        // Check if reservation is too early
        if (TooEarlyToMakeReservation(reservation))
            return BadRequest("Cannot update a reservation that starts in more than a week from now.");

        // Check if reservation is past
        if (IsReservationPast(reservation))
            return BadRequest("Cannot update a reservation that has already started.");

        // Check if maximum reservations per user exceeded
        if (MaximumReservationsPerUserExceeded(_context, reservation))
            return BadRequest("User cannot have more than 3 active reservations on the same day.");

        // Check if user already has a reservation that overlaps with the updated reservation
        if (UserAlreadyHasReservation(_context, reservation))
            return BadRequest("User already has a reservation that overlaps with the updated reservation.");

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservation(Guid id)
    {
        var reservation = await _context.Reservations.FindAsync(id);

        if (reservation == null)
            return NotFound();

        if (TooLateToCancel(reservation))
            return BadRequest("Cannot cancel a reservation less than 30 minutes before it starts.");

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private static bool OverlappingReservationsInRoom(AppDbContext context, Reservation reservation)
    {
        return context.Reservations.Any(r =>
            r.RoomId == reservation.RoomId &&
            r.Id != reservation.Id &&
            r.Start < reservation.End &&
            r.End > reservation.Start);
    }

    private static bool ReservationExceedsMaxDuration(Reservation reservation)
    {
        return reservation.End - reservation.Start > TimeSpan.FromHours(2);
    }

    private static bool TooEarlyToMakeReservation(Reservation reservation)
    {
        return reservation.Start > DateTime.UtcNow.AddDays(7);
    }

    private static bool IsReservationPast(Reservation reservation)
    {
        return reservation.Start < DateTime.UtcNow;
    }

    private static bool TooLateToCancel(Reservation reservation)
    {
        return reservation.Start < DateTime.UtcNow.AddMinutes(30);
    }

    private static bool MaximumReservationsPerUserExceeded(AppDbContext context, Reservation reservation)
    {
        var userReservationsCount = context.Reservations.Count(r =>
            r.UserId == reservation.UserId &&
            r.Start.Date == reservation.Start.Date &&
            r.Id != reservation.Id);

        return userReservationsCount >= 3;
    }

    private static bool UserAlreadyHasReservation(AppDbContext context, Reservation reservation)
    {
        var boolean = context.Reservations.Any(r =>
            r.UserId == reservation.UserId &&
            r.Start.Date == reservation.Start.Date &&
            r.Start.Hour <= reservation.End.Hour &&
            r.End.Hour >= reservation.Start.Hour &&
            r.Id != reservation.Id);

        return boolean;
    }
}
