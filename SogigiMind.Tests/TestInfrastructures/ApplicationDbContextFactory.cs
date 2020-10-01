using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;

namespace SogigiMind.TestInfrastructures
{
    internal static class ApplicationDbContextFactory
    {
        public static ApplicationDbContext CreateInMemory()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("test")
                .Options;
            return new ApplicationDbContext(options);
        }
    }
}
