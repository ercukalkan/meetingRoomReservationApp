using Data.Entities;
using System.Text.Json;

namespace Data.Context.DbSeeder;

public class DbSeeder
{
    public static async Task SeedData(AppDbContext context)
    {
        context.Database.EnsureCreated();

        var seedFiles = Directory.GetFiles("../Data/DbSeeder/SeedData", "*.json");

        foreach (var file in seedFiles)
        {
            var typeName = Path.GetFileNameWithoutExtension(file);
            var jsonData = await File.ReadAllTextAsync(file);

            switch (typeName)
            {
                case "rooms":
                    if (!context.Rooms.Any())
                    {
                        var rooms = JsonSerializer.Deserialize<List<Room>>(jsonData);
                        if (rooms == null) return;
                        context.Rooms.AddRange(rooms);
                    }
                    break;
                case "users":
                    if (!context.Users.Any())
                    {
                        var users = JsonSerializer.Deserialize<List<User>>(jsonData);
                        if (users == null) return;
                        context.Users.AddRange(users);
                    }
                    break;
                case "equipments":
                    if (!context.Equipments.Any())
                    {
                        var equipments = JsonSerializer.Deserialize<List<Equipment>>(jsonData);
                        if (equipments == null) return;
                        context.Equipments.AddRange(equipments);
                    }
                    break;
                case "reservations":
                    break;
                case "recurringReservations":
                    break;
                default:
                    throw new InvalidOperationException($"Unknown type in seed data: {typeName}");
            }
        }
        await context.SaveChangesAsync();
    }
}