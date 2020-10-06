using System;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using SogigiMind.Authentication;
using SogigiMind.Data;
using SogigiMind.Logics;
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
            services.AddAuthentication(AccessTokenAuthenticationHandler.DefaultAuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>(AccessTokenAuthenticationHandler.DefaultAuthenticationScheme, null);

            services.AddAuthorization(options => options.AddEndUserPolicy());

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(this.Configuration.GetConnectionString("Default"))
                    .UseSnakeCaseNamingConvention(),
                ServiceLifetime.Transient,
                ServiceLifetime.Singleton
            );

            services.AddDbConnectionProvider(serviceProvider =>
            {
                // ���N�G�X�g�̃X�R�[�v�Ɍ��炸������ ApplicationDbContext ���쐬����
                var options = serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
                return new ApplicationDbContext(options);
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo() { Title = "SogigiMind", Version = AppVersion.InformationalVersion });
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "SogigiMind.xml"));
                c.AddSogigiMindAuthenticationOperationFilter();
            });

            this.ConfigureOptions(services);
            this.ConfigureBusinessServices(services);
            this.ConfigureRepositories(services);
            this.ConfigureUseCases(services);
        }

        private void ConfigureOptions(IServiceCollection services)
        {
            services.AddOptions<DashboardLoginOptions>()
                .Bind(this.Configuration.GetSection("SogigiMind:DashboardLogin"));

            services.AddOptions<ThumbnailOptions>()
                .Bind(this.Configuration.GetSection("SogigiMind:Thumbnail"))
                .ValidateDataAnnotations();
        }

        private void ConfigureBusinessServices(IServiceCollection services)
        {
            services.AddSingleton<IBlobServiceFactory, DefaultBlobServiceFactory>();
            services.AddTransient(serviceProvider => serviceProvider
                .GetRequiredService<IBlobServiceFactory>()
                .CreateBlobService(serviceProvider.GetService<ApplicationDbContext>()));
            services.AddRemoteFetchService();
        }

        private void ConfigureRepositories(IServiceCollection services)
        {
            services.AddTransient<AccessTokenRepository>();
            services.AddTransient<RemoteImageRepository>();
        }

        private void ConfigureUseCases(IServiceCollection services)
        {
            services.AddTransient<UseCases.AccessToken.CreateDashboardTokenUseCase>();
            services.AddTransient<UseCases.Administration.CreateTokenUseCase>();
            services.AddTransient<UseCases.Sensitivity.DeletePersonalSensitivitiesUseCase>();
            services.AddTransient<UseCases.Sensitivity.EstimateSensitivityUseCase>();
            services.AddTransient<UseCases.Sensitivity.SetSensitivityUseCase>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
