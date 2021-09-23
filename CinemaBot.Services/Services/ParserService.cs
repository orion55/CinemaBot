using System;
using System.Net;
using CinemaBot.Services.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace CinemaBot.Services.Services
{
    public class ParserService : IParserService
    {
        private readonly ILogger _log;
        private readonly bool _useProxy;
        private readonly ProxyService _serviceProxy;
        private Proxy _currentProxy;
        private string[] _items;

        public ParserService(ILogger log, IConfiguration configuration)
        {
            _log = log;

            _useProxy = Convert.ToBoolean(configuration["useProxy"]);

            if (_useProxy)
            {
                _serviceProxy = new ProxyService();
                _currentProxy = _serviceProxy.GetRandomProxy() ?? throw new Exception("The proxy list is empty");
            }
        }

        public void MainPageParser(string url)
        {
            if (String.IsNullOrEmpty(url))
                throw new Exception("The \"url\" value is empty");

            _log.Information("Parse url: {0}", url);

            int i = 0;
            bool isStarting = false;
            do
            {
                try
                {
                    HtmlWeb web = new HtmlWeb();

                    var doc = _useProxy
                        ? web.Load(url, _currentProxy.ProxyHost, _currentProxy.ProxyPort, _currentProxy.UserId,
                            _currentProxy.Password)
                        : web.Load(url);

                    var nodes = doc.DocumentNode.SelectNodes("//a[@class='topictitle']");

                    int count = nodes.Count;
                    if (count > 0)
                    {
                        _items = new string[count];

                        for (int j = 0; j < count; j++)
                        {
                            _items[j] = nodes[j].InnerText;
                            // _items[j] = nodes[j].Attributes["href"].Value;
                        }

                        Console.WriteLine("Result: {0}", String.Join(", ", _items));
                    }
                    else
                    {
                        throw new Exception("The parsing result is empty");
                    }
                }
                catch (WebException ex)
                {
                    if (_useProxy)
                    {
                        i++;
                        _serviceProxy.SetBadProxy(_currentProxy.Id);
                        isStarting = true;
                        if (i == _serviceProxy.Count)
                        {
                            _serviceProxy.SaveProxy();
                            throw new Exception("Link " + url + " loading failed");
                        }

                        _currentProxy = _serviceProxy.GetRandomProxy() ??
                                        throw new Exception("The proxy list is empty");
                    }

                    _log.Error(ex.Message);
                }
            } while (isStarting);

            if (_useProxy) _serviceProxy.SaveProxy();
        }
    }
}