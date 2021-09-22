using System;
using CinemaBot.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CinemaBot
{
    public class Job
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _config;
        private readonly IParserService _parser;

        public Job(ILogger logger, IConfiguration config, IParserService parserService)
        {
            _logger = logger;
            _config = config;
            _parser = parserService;
        }

        public void Run()
        {
            _logger.Information("Parsing started");
            string[] urls = _config.GetSection("urls").Get<string[]>();
            try
            {
                _parser.Parser(urls[0]);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, ex.Message);
            }
        }
    }
}