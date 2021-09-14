using System;
using System.Collections.Generic;
using System.IO;
using CinemaBot.Configurations;
using CinemaBot.Data;
using CinemaBot.Services;
using CinemaBot.Services.Interfaces;
using CinemaBot.Services.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NpgsqlTypes;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;

namespace CinemaBot
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        public ILogger Logger { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();
            // Console.WriteLine(string.Join("\n", this.configuration.GetSection("urls").Get<string[]>()));
            
            string tableName = "logs";

            IDictionary<string, ColumnWriterBase> columnWriters = new Dictionary<string, ColumnWriterBase>
            {
                {"message", new RenderedMessageColumnWriter(NpgsqlDbType.Text) },
                {"message_template", new MessageTemplateColumnWriter(NpgsqlDbType.Text) },
                {"level", new LevelColumnWriter(true, NpgsqlDbType.Varchar) },
                {"raise_date", new TimestampColumnWriter(NpgsqlDbType.Timestamp) },
                {"exception", new ExceptionColumnWriter(NpgsqlDbType.Text) },
                {"properties", new LogEventSerializedColumnWriter(NpgsqlDbType.Jsonb) },
                {"props_test", new PropertiesColumnWriter(NpgsqlDbType.Jsonb) },
                {"machine_name", new SinglePropertyColumnWriter("MachineName", PropertyWriteMethod.ToString, NpgsqlDbType.Text, "l") }
            };

            string connectionstring = _configuration.GetConnectionString("DefaultConnection");
            
            Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.PostgreSQL(connectionstring, tableName, columnWriters, LogEventLevel.Information, 
                    null, null, 30, null, true,"",true,false)
                .CreateLogger();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient(provider => _configuration);

            services.AddTransient<IParserService>(s => new ParserService(Logger));

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_configuration.GetConnectionString("DefaultConnection")));

            services.AddHangfire(config =>
                config.UsePostgreSqlStorage(_configuration.GetConnectionString("DefaultConnection")));
            GlobalConfiguration.Configuration
                .UsePostgreSqlStorage(_configuration.GetConnectionString("DefaultConnection"))
                .WithJobExpirationTimeout(TimeSpan.FromDays(7));
            services.AddHangfireServer();
            
            services.AddSingleton(Logger);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env,IBackgroundJobClient backgroundJobClient, 
            IParserService parserService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var options = new DashboardOptions
            {
                Authorization = new[]
                {
                    new DashboardAuthorization(new[]
                    {
                        new HangfireUserCredentials
                        {
                            Username = _configuration.GetSection("HangfireCredentials:UserName").Value,
                            Password = _configuration.GetSection("HangfireCredentials:Password").Value
                        }
                    })
                }
            };
            app.UseHangfireDashboard("/hangfire", options);
            
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
            });
            
            Job jobscheduler = new Job(Logger, _configuration, parserService);
            backgroundJobClient.Enqueue(() => jobscheduler.Run());
            // recurringJobManager.AddOrUpdate("Insert Employee : Runs Every 30 Sec", () => jobscheduler.Run(), "*/30 * * * * *");
        }
    }
}