using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SogigiMind.Data;

namespace SogigiMind
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            var config = host.Services.GetRequiredService<IConfiguration>();
            if (config.GetValue<bool>("SogigiMind:Migrate") == true)
            {
                using var scope = host.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // appsettings.override.json is for local debug
                .ConfigureAppConfiguration(builder => builder.AddJsonFile("appsettings.override.json", true, true))
                .ConfigureLogging(builder => builder.AddJsonStream())
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
