using System;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using SogigiMind.BackgroundServices;
using SogigiMind.Data;
using SogigiMind.DataAccess;
using SogigiMind.Logics;
using SogigiMind.Options;
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

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(this.Configuration.GetConnectionString("Default"))
                    .UseSnakeCaseNamingConvention());

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo() { Title = "SogigiMind", Version = AppVersion.InformationalVersion });
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "SogigiMind.xml"));
            });

            services.AddSingleton<ISystemClock, SystemClock>();

            this.ConfigureOptions(services);
            this.ConfigureBusinessServices(services);
            this.ConfigureDataAccessObjects(services);
            this.ConfigureUseCases(services);
            this.ConfigureBackgroundServices(services);
        }

        private void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions<ThumbnailOptions>()
                .Bind(this.Configuration.GetSection("SogigiMind:Thumbnail"))
                .ValidateDataAnnotations();
        }

        private void ConfigureBusinessServices(IServiceCollection services)
        {
            services.AddScoped<IBlobService, DefaultBlobService>();
            services.AddRemoteFetchService();
            services.AddScoped<IThumbnailCreationService, DefaultThumbnailCreationService>();
            services.AddThumbnailQueueService();
        }

        private void ConfigureDataAccessObjects(IServiceCollection services)
        {
            services.AddScoped<IFetchAttemptDao, EfFetchAttemptDao>();
            services.AddScoped<IRemoteImageDao, PostgresRemoteImageDao>();
        }

        private void ConfigureUseCases(IServiceCollection services)
        {
            services.AddTransient<UseCases.Sensitivity.DeletePersonalSensitivitiesUseCase>();
            services.AddTransient<UseCases.Sensitivity.EstimateSensitivityUseCase>();
            services.AddTransient<UseCases.Sensitivity.SetSensitivityUseCase>();
        }

        private void ConfigureBackgroundServices(IServiceCollection services)
        {
            services.AddHostedService<ThumbnailBackgroundService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SogigiMind"));

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
