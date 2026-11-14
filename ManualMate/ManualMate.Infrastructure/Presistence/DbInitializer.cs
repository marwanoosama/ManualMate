using ManualMate.Domain.Models;

namespace ManualMate.Infrastructure.Presistence
{
    public static partial class DbInitializer
    {
        // Runs automatically at API startup (Program.cs) when the Items table is empty.
        // Seeds exactly ONE item so you can immediately test the app.
        //
        // The other sample items (PS5, Sharp AC) were moved to DbInitializer.Samples.cs.
        // They are NOT seeded automatically anymore - use them manually if you want.
        public static async Task SeedAsync(ManualMateDbContext context)
        {
            if (!context.Items.Any())
            {
                var item = new Item
                {
                    Name = "Samsung Galaxy S24",
                    Description = "A series of high-end Android-based smartphones developed, manufactured, and marketed by Samsung Electronics as part of its flagship Galaxy S series",
                    FilePath = "wwwroot/Files/Galaxy_S24_Manual.pdf",
                    LastUpdated = DateTime.UtcNow
                };

                context.Items.Add(item);
                await context.SaveChangesAsync();
            }
        }
    }
}
