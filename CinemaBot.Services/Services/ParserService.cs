using System;
using System.Net;
using CinemaBot.Services.Interfaces;
using Serilog;
using HtmlAgilityPack;

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

            string RefURL = "https://nnmclub.to/forum/viewforum.php?f=319";
            string myProxyIP = "31.40.253.129"; //check this is still available
            int myPort = 8085;
            string userId = string.Empty; //leave it blank
            string password = string.Empty;
            try
            {
                HtmlWeb web = new HtmlWeb();
                var doc = web.Load(url, myProxyIP, myPort, userId, password);
                // Console.WriteLine(doc.DocumentNode.InnerHtml);
            }
            catch (WebException ex)
            {
                _log.Error(ex.Message);
            }
        }
    }
}