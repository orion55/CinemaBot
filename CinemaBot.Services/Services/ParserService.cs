﻿using System;
using System.Net;
using System.Text;
using System.Web;
using CinemaBot.Services.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Serilog;
using static System.Convert;

namespace CinemaBot.Services.Services
{
    public class ParserService : IParserService
    {
        const string NnmClub = "https://nnmclub.to/forum/";
        
        private readonly ILogger _log;
        private readonly bool _useProxy;
        private readonly ProxyService _serviceProxy;
        private Proxy _currentProxy;
        private int[] _items;

        public ParserService(ILogger log, IConfiguration configuration)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _log = log;

            _useProxy = ToBoolean(configuration["useProxy"]);

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
                        _items = new int[count];

                        for (int j = 0; j < count; j++)
                        {
                            // _items[j] = DecodeText(nodes[j].InnerText);
                            // _items[j] = nodes[j].InnerText;
                            _items[j] = GetParamFromUrl(nodes[j].Attributes["href"].Value);
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

        private string DecodeText(string text)
        {
            var encFrom = Encoding.GetEncoding("windows-1251");
            var encTo = Encoding.GetEncoding("utf-8");
            byte[] bytes = encFrom.GetBytes(text);
            bytes = Encoding.Convert(encFrom, encTo, bytes);
            return encTo.GetString(bytes);
        }

        private int GetParamFromUrl(string url)
        {
            Uri myUri = new Uri(NnmClub + url);
            string param = HttpUtility.ParseQueryString(myUri.Query).Get("t");
            return ToInt32(param);
        }
    }
}