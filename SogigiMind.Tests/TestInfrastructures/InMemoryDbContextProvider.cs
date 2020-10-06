using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SogigiMind.Data;
using SogigiMind.Services;

namespace SogigiMind.TestInfrastructures
{
    internal class InMemoryDbContextProvider : DbConnectionProvider<ApplicationDbContext>
    {
        public InMemoryDbContextProvider()
            : base(CreateConnectionFactory())
        { }

        private static Func<ApplicationDbContext> CreateConnectionFactory()
        {
            var databaseRoot = new InMemoryDatabaseRoot();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("test", databaseRoot)
                .Options;
            return () => new ApplicationDbContext(options);
        }
    }
}
