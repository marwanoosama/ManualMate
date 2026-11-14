using ManualMate.Domain.Models;

namespace ManualMate.Infrastructure.Presistence
{
    // ---------------------------------------------------------------------------
    // Extra sample items (not seeded automatically anymore).
    //
    // The automatic seeding in DbInitializer.SeedAsync now creates only the
    // Samsung Galaxy S24 item. These two were part of the old 3-item seed.
    //
    // They are kept here so YOU can add them manually when testing the app, e.g.:
    //
    //   1) Uncomment the call in Program.cs right after DbInitializer.SeedAsync:
    //          await DbInitializer.AddSampleItemsAsync(context);
    //   2) Or call it once from anywhere you like (debug console, a temp endpoint...).
    //
    // The PDFs already exist under ManualMate.API/wwwroot/Files/.
    // NOTE: like the seeded item, these are created with Status = Pending and
    // without embeddings - they are NOT processed on startup. They are useful
    // mainly to test CRUD, or as FilePath targets if you want to trigger
    // processing yourself.
    // ---------------------------------------------------------------------------
    public static partial class DbInitializer
    {
        public static async Task AddSampleItemsAsync(ManualMateDbContext context)
        {
            var existingNames = context.Items.Select(i => i.Name).ToHashSet();

            var samples = new List<Item>
            {
                new Item
                {
                    Name = "PS5",
                    Description = "The PlayStation 5 (PS5) is a home video game console developed by Sony Interactive Entertainment. It was announced as the successor to the PlayStation 4 in April 2019, was launched on November 12, 2020",
                    FilePath = "wwwroot/Files/PS5_Manual.pdf",
                    LastUpdated = DateTime.UtcNow
                },
                new Item
                {
                    Name = "Sharp AC",
                    Description = "Sharp air conditioners are Japanese-made electronic devices known for features like Plasmacluster ion technology to purify the air, Gentle Air Cool (Coanda) mode that directs air to the ceiling to prevent cold drafts, and energy-efficient Inverter technology",
                    FilePath = "wwwroot/Files/Sharp_Ac_Manual.pdf",
                    LastUpdated = DateTime.UtcNow
                }
            };

            foreach (var sample in samples.Where(s => !existingNames.Contains(s.Name)))
            {
                context.Items.Add(sample);
            }

            await context.SaveChangesAsync();
        }
    }
}
