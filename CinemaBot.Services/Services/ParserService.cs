﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CinemaBot.Core;
using CinemaBot.Models;
using CinemaBot.Services.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Serilog;
using static System.Convert;

namespace CinemaBot.Services.Services
{
    public class ParserService : IParserService
    {
        private readonly ILogger _log;
        private readonly bool _useProxy;
        private readonly ProxyService _serviceProxy;
        private Proxy _currentProxy;
        private readonly int[] _exceptionIds;
        private const int maxCount = 3;

        public ParserService(ILogger log, IConfiguration configuration)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _log = log;

            _useProxy = ToBoolean(configuration["useProxy"]);
            _exceptionIds = configuration.GetSection("exceptionIds").Get<int[]>();

            if (_useProxy)
            {
                _serviceProxy = new ProxyService();
                _currentProxy = _serviceProxy.GetRandomProxy() ?? throw new Exception("The proxy list is empty");
            }
        }

        public async void Parser(string url)
        {
            int[] ids = MainPageParser(url);
            Console.WriteLine("ids: {0}", String.Join(", ", ids));
            List<UrlModel> links = await SecondPagesParser(ids);
            Console.WriteLine("links: {0}", String.Join("\n ", links));
        }

        private int[] MainPageParser(string url)
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
                        int[] ids = new int[count];

                        for (int j = 0; j < count; j++)
                            ids[j] = GetParamFromUrl(nodes[j].Attributes["href"].Value);

                        return ids.Except(_exceptionIds).ToArray();
                    }

                    throw new Exception("The parsing result is empty");
                }
                catch (Exception ex)
                {
                    if (ex is WebException)
                    {
                        if (_useProxy)
                        {
                            i++;
                            _log.Error("{0} is bad", propertyValue: _currentProxy.ProxyHost);
                            isStarting = true;
                            if (i == _serviceProxy.Count)
                            {
                                _serviceProxy.SaveProxy();
                                throw new Exception("Link " + url + " loading failed");
                            }

                            _currentProxy = _serviceProxy.GetRandomProxy() ??
                                            throw new Exception("The proxy list is empty");
                        }
                    }

                    _log.Error(ex.Message);
                }
            } while (isStarting);

            return null;
        }

        private async Task<List<UrlModel>> SecondPagesParser(int[] ids)
        {
            if (ids == null || ids.Length == 0)
                throw new Exception("The array of ids is empty");
            var ids10 = ids.Take(10).ToArray();
            // var ids10 = ids;

            var tasks = new List<Task>();

            try
            {
                foreach (var id in ids10)
                    tasks.Add(Task.Run(() => GetUrl(id)));

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }

            List<UrlModel> results = new List<UrlModel>();
            foreach (var task in tasks)
            {
                var result = ((Task<UrlModel>)task).Result;
                if (result != null)
                    results.Add(result);
            }

            return results;
        }

        private UrlModel GetUrl(int id)
        {
            var proxy = _serviceProxy.GetRandomProxy() ??
                        throw new Exception("The proxy list is empty");
            int i = 0;
            bool isStarting = false;
            var url = Constants.NnmClubTopic + "?t=" + Convert.ToString(id);
            HtmlWeb web = new HtmlWeb();

            do
            {
                try
                {
                    var doc = _useProxy
                        ? web.Load(url, proxy.ProxyHost, proxy.ProxyPort, proxy.UserId,
                            proxy.Password)
                        : web.Load(url);

                    string title = doc.DocumentNode.SelectSingleNode("//a[@class='maintitle']").InnerText;
                    var nodeImg = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");

                    string imgUrl = "";
                    if (nodeImg != null)
                        imgUrl = nodeImg.Attributes["content"].Value;
                    else
                    {
                        HtmlNodeCollection nodesImg =
                            doc.DocumentNode.SelectNodes("//var[@class='postImg postImgAligned img-right']");

                        if (nodesImg != null)
                        {
                            HtmlNode imgNode = nodesImg[0];
                            string titleImg = imgNode.Attributes["title"].Value;
                            string linkStr = "?link=";
                            int index = titleImg.IndexOf(linkStr, StringComparison.Ordinal) + linkStr.Length;
                            imgUrl = titleImg.Substring(index, titleImg.Length - index);
                        }
                    }

                    var urlModel = new UrlModel(id, title, imgUrl);
                    return urlModel;
                }
                catch (Exception ex)
                {
                    if (_useProxy)
                    {
                        i++;
                        isStarting = true;
                        if (i == maxCount)
                        {
                            _log.Error("Link {0} loading failed", url);
                            return null;
                        }

                        proxy = _serviceProxy.GetRandomProxy() ??
                                throw new Exception("The proxy list is empty");
                    }
                    else
                    {
                        _log.Error("Link {0} loading failed", url);
                        _log.Error(ex.Message);
                        return null;
                    }
                }
            } while (isStarting);

            return null;
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
            Uri myUri = new Uri(Constants.NnmClub + url);
            string param = HttpUtility.ParseQueryString(myUri.Query).Get("t");
            return ToInt32(param);
        }
    }
}