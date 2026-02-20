using Data.Entities;
using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data.DTOs;
using Data.Filtering;
using Data.Response;
using Core.Exceptions;
using Core.Helper;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController(AppDbContext _context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ResponseSchema<IReadOnlyList<ReservationResponseDTO>>>> GetReservations([FromQuery] FilterParams filterParams)
    {
        if (filterParams.Start.HasValue && filterParams.End.HasValue && filterParams.Start >= filterParams.End)
            throw new BadRequestException("Start must be earlier than End.");

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

        return Ok(new ResponseSchema<IReadOnlyList<ReservationResponseDTO>>
        {
            Message = "Reservations retrieved successfully.",
            Success = true,
            Data = reservations
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseSchema<ReservationResponseDTO>>> GetReservation(Guid id)
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
            .FirstOrDefaultAsync(r => r.Id == id)
            ??
            throw new NotFoundException($"Reservation with ID {id} not found.");

        return Ok(new ResponseSchema<ReservationResponseDTO>
        {
            Message = "Reservation retrieved successfully.",
            Success = true,
            Data = reservation
        });
    }

    [HttpPost]
    public async Task<ActionResult<ResponseSchema<ReservationResponseDTO>>> CreateReservation(ReservationDTO dto)
    {
        if (dto == null)
            throw new BadRequestException("Reservation data is required.");

        var newReservation = new Reservation
        {
            Start = dto.Start,
            End = dto.End,
            RoomId = dto.RoomId,
            UserId = dto.UserId,
            RecurringReservationId = dto.RecurringReservationId
        };

        ReservationHelper.ValidationCheck(_context, newReservation);

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
               new ResponseSchema<ReservationResponseDTO>
               {
                   Message = "Reservation created successfully.",
                   Success = true,
                   Data = createdReservation
               }
           );
        }

        throw new BadRequestException("Wrong object structure.");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReservation(Guid id, ReservationDTO dto)
    {
        if (id != dto.Id)
            throw new BadRequestException("ID mismatch.");

        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new NotFoundException($"Reservation with Id {id} not found.");

        reservation.Start = dto.Start;
        reservation.End = dto.End;
        reservation.RoomId = dto.RoomId;
        reservation.UserId = dto.UserId;

        ReservationHelper.ValidationCheck(_context, reservation);

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservation(Guid id)
    {
        var reservation = await _context.Reservations.FindAsync(id)
            ?? throw new NotFoundException($"Reservation with ID {id} not found.");

        if (ReservationHelper.TooLateToCancel(reservation))
            throw new BadRequestException("Cannot cancel a reservation less than 30 minutes before it starts.");

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
