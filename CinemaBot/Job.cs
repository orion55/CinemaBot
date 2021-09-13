using System;
using Serilog;

namespace CinemaBot
{
    public class Job
    {
        private readonly ILogger _logger;
        public Job(ILogger logger)
        {
            _logger = logger;
        }

        public void Run()
        {
            _logger.Information("Job started");
        }
    }
}