using System;
using System.Net;
using CinemaBot.Services.Interfaces;
using Serilog;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;

namespace CinemaBot.Services.Services
{
    public class ParserService : IParserService
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

            bool useProxy = Convert.ToBoolean(_configuration["useProxy"]);

            ProxyService serviceProxy = new ProxyService();

            int i = 0;
            bool isStarting = false;
            do
            {
                Proxy proxy = serviceProxy.GetRandomProxy() ?? throw new Exception("The proxy list is empty");
                try
                {
                    HtmlWeb web = new HtmlWeb();
                    var doc = useProxy
                        ? web.Load(url, proxy.ProxyHost, proxy.ProxyPort, proxy.UserId, proxy.Password)
                        : web.Load(url);
                    // Console.WriteLine(doc.DocumentNode.InnerHtml);
                }
                catch (WebException ex)
                {
                    if (useProxy)
                    {
                        i++;
                        serviceProxy.SetBadProxy(proxy.Id);
                        isStarting = true;
                        if (i == serviceProxy.Count)
                        {
                            serviceProxy.SaveProxy();
                            throw new Exception("Link " + url + " loading failed");
                        }
                    }

                    _log.Error(ex.Message);
                }
            } while (isStarting);

            if (useProxy) serviceProxy.SaveProxy();
        }
    }
}