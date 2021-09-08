using System;
using System.IO;
using CinemaBot.Data;
using CinemaBot.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HangfireBasicAuthenticationFilter;

namespace CinemaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly Job jobscheduler = new Job();

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();
            // Console.WriteLine(string.Join("\n", this.configuration.GetSection("urls").Get<string[]>()));
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<IConfiguration>(provider => _configuration);

            services.AddTransient<ParserService>();

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection")));

            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(_configuration.GetConnectionString("DefaultConnection")));
            GlobalConfiguration.Configuration
                .UsePostgreSqlStorage(_configuration.GetConnectionString("DefaultConnection"))
                .WithJobExpirationTimeout(TimeSpan.FromDays(7));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var options = new DashboardOptions
            {
                Authorization = new[]
                {
                    new HangfireCustomBasicAuthenticationFilter
                    {
                        User = _configuration.GetSection("HangfireCredentials:UserName").Value,
                        Pass = _configuration.GetSection("HangfireCredentials:Password").Value
                    }
                }
            };
            app.UseHangfireDashboard("/hangfire", options);
            app.UseHangfireServer();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
        }
    }
}