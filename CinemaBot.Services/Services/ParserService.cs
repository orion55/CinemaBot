using System;
using CinemaBot.Services.Interfaces;
using Serilog;

namespace CinemaBot.Services.Services
{
    public class ParserService: IParserService
    {
        private readonly ILogger _log;

        public ParserService(ILogger log)
        {
            _log = log;
        }

        public void Parser(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new Exception("The \"url\" value is empty");

            _log.Information("Parse url: {0}", url);
        }
    }
}