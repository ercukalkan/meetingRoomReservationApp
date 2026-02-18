using Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Data.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<RecurringReservation> RecurringReservations => Set<RecurringReservation>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicitly configure relationships and constraints for entities
        // Configure Reservation entity
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasOne(r => r.Room)
                .WithMany(room => room.Reservations)
                .HasForeignKey(r => r.RoomId)
                .HasConstraintName("FK_Reservation_Room")
                .IsRequired();

            entity.HasOne(r => r.User)
                .WithMany(u => u.Reservations)
                .HasForeignKey(r => r.UserId)
                .HasConstraintName("FK_Reservation_User")
                .IsRequired();

            entity.HasOne(r => r.RecurringReservation)
                .WithMany(rr => rr.Reservations)
                .HasForeignKey(r => r.RecurringReservationId)
                .HasConstraintName("FK_Reservation_RecurringReservation")
                .IsRequired(false);

            entity.Property(r => r.Start)
                .IsRequired();

            entity.Property(r => r.End)
                .IsRequired();
        });

        // Configure many-to-many relationship between Room and Equipment
        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasMany(r => r.Equipments)
                .WithMany(e => e.Rooms)
                .UsingEntity<Dictionary<string, object>>(
                    "RoomEquipment",
                    j => j
                        .HasOne<Equipment>()
                        .WithMany()
                        .HasForeignKey("EquipmentId")
                        .HasConstraintName("FK_RoomEquipment_Equipment")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Room>()
                        .WithMany()
                        .HasForeignKey("RoomId")
                        .HasConstraintName("FK_RoomEquipment_Room")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("RoomId", "EquipmentId");
                        j.ToTable("RoomEquipment");
                    }
                );
        });

        // Configure RecurringReservation entity
        modelBuilder.Entity<RecurringReservation>(entity =>
        {
            entity.HasOne(rr => rr.Room)
                .WithMany(room => room.RecurringReservations)
                .HasForeignKey(rr => rr.RoomId)
                .HasConstraintName("FK_RecurringReservation_Room")
                .IsRequired();

            entity.HasOne(rr => rr.User)
                .WithMany(u => u.RecurringReservations)
                .HasForeignKey(rr => rr.UserId)
                .HasConstraintName("FK_RecurringReservation_User")
                .IsRequired();

            entity.Property(rr => rr.Start)
                .IsRequired();

            entity.Property(rr => rr.End)
                .IsRequired();
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}