using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SogigiMind.Data;
using SogigiMind.Infrastructures;
using SogigiMind.Options;
using SogigiMind.Repositories;
using SogigiMind.Services;

namespace SogigiMind
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddAuthentication(TokenAuthenticationHandler.DefaultAuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(TokenAuthenticationHandler.DefaultAuthenticationScheme, null);

            services.AddSingleton<IMongoDatabase>(serviceProvider =>
            {
                var databaseOptions = serviceProvider.GetRequiredService<IOptionsMonitor<DatabaseOptions>>().CurrentValue;
                var client = new MongoClient(databaseOptions.ConnectionString);
                return client.GetDatabase(databaseOptions.Database);
            });

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(this.Configuration.GetConnectionString("Default"))
                    .UseSnakeCaseNamingConvention()
            );

            this.ConfigureOptions(services);
            this.ConfigureBusinessServices(services);
            this.ConfigureRepositories(services);
        }

        private void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions<DatabaseOptions>().ValidateDataAnnotations();

            if (this.Configuration.GetSection("Database") is { } databaseSection)
                services.Configure<DatabaseOptions>(databaseSection);

            services.AddOptions<ThumbnailOptions>().ValidateDataAnnotations();

            if (this.Configuration.GetSection("Thumbnail") is { } thumbnailSection)
                services.Configure<ThumbnailOptions>(thumbnailSection);

            if (this.Configuration.GetSection("Tokens") is { } tokensSection)
                services.Configure<TokenOptions>(tokensSection);
        }

        private void ConfigureBusinessServices(IServiceCollection services)
        {
            services.AddSingleton<PersonalSensitivityService>();
            services.AddSingleton<ThumbnailService>();
        }

        private void ConfigureRepositories(IServiceCollection services)
        {
            services.AddSingleton<IFetchStatusRepository, FetchStatusRepository>();
            services.AddSingleton<IPersonalSensitivityRepository, PersonalSensitivityRepository>();
            services.AddSingleton<IThumbnailRepository, ThumbnailRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
