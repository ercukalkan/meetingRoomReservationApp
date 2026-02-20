using Core.Exceptions;
using Data.Context;
using Data.Entities;

namespace Core.Helper;

public static class ReservationHelper
{
    public static bool OverlappingReservationsInRoom(AppDbContext context, Reservation reservation)
    {
        return context.Reservations.Any(r =>
            r.RoomId == reservation.RoomId &&
            r.Id != reservation.Id &&
            r.Start < reservation.End &&
            r.End > reservation.Start);
    }

    public static bool ReservationExceedsMaxDuration(Reservation reservation)
    {
        return reservation.End - reservation.Start > TimeSpan.FromHours(2);
    }

    public static bool TooEarlyToMakeReservation(Reservation reservation)
    {
        return reservation.Start > DateTime.UtcNow.AddDays(7);
    }

    public static bool IsReservationPast(Reservation reservation)
    {
        return reservation.Start < DateTime.UtcNow;
    }

    public static bool TooLateToCancel(Reservation reservation)
    {
        return reservation.Start < DateTime.UtcNow.AddMinutes(30);
    }

    public static bool MaximumReservationsPerUserExceeded(AppDbContext context, Reservation reservation)
    {
        var userReservationsCount = context.Reservations.Count(r =>
            r.UserId == reservation.UserId &&
            r.Start.Date == reservation.Start.Date &&
            r.Id != reservation.Id);

        return userReservationsCount >= 3;
    }

    public static bool UserAlreadyHasReservation(AppDbContext context, Reservation reservation)
    {
        var boolean = context.Reservations.Any(r =>
            r.UserId == reservation.UserId &&
            r.Start.Date == reservation.Start.Date &&
            r.Start.Hour <= reservation.End.Hour &&
            r.End.Hour >= reservation.Start.Hour &&
            r.Id != reservation.Id);

        return boolean;
    }

    public static void ValidationCheck(AppDbContext context, Reservation reservation)
    {
        // Check for overlapping reservations for the same room
        if (OverlappingReservationsInRoom(context, reservation))
            throw new BadRequestException("The reservation overlaps with an existing reservation for the same room.");

        // Check for maximum duration
        if (ReservationExceedsMaxDuration(reservation))
            throw new BadRequestException("The reservation exceeds the maximum allowed duration of 2 hours.");

        // Check if reservation is too early
        if (TooEarlyToMakeReservation(reservation))
            throw new BadRequestException("Cannot create a reservation that starts in more than a week from now.");

        // Check if reservation is past
        if (IsReservationPast(reservation))
            throw new BadRequestException("Cannot create a reservation from the past.");

        // Check if maximum reservations per user exceeded
        if (MaximumReservationsPerUserExceeded(context, reservation))
            throw new BadRequestException("User cannot have more than 3 active reservations on the same day.");

        // Check if user already has a reservation that overlaps with the new reservation
        if (UserAlreadyHasReservation(context, reservation))
            throw new BadRequestException("User already has a reservation that overlaps with the new reservation.");
    }
}