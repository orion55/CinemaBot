using System;
using System.Net;
using CinemaBot.Services.Interfaces;
using Serilog;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace CinemaBot.Services.Services
{
    public class ParserService: IParserService
    {
        private readonly ILogger _log;
        private readonly IConfiguration _configuration;

        public ParserService(ILogger log, IConfiguration configuration)
        {
            _log = log;
            _configuration = configuration;
        }

        public void Parser(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new Exception("The \"url\" value is empty");

            _log.Information("Parse url: {0}", url);

            Boolean useProxy = Convert.ToBoolean(_configuration["useProxy"]);
            if (useProxy)
            {
                ProxyService serviceProxy = new ProxyService();
                Proxy proxy = serviceProxy.GetRandomProxy();
                Console.WriteLine(proxy);
                serviceProxy.SetBadProxy(proxy.Id);
                serviceProxy.SaveProxy();
                
            }

            /*string RefURL = "https://nnmclub.to/forum/viewforum.php?f=319";
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
            }*/
        }
    }
}