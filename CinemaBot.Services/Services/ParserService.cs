using System;
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
                        int[] ids = new int[count];

                        for (int j = 0; j < count; j++)
                            ids[j] = GetParamFromUrl(nodes[j].Attributes["href"].Value);

                        ids = ids.Except(_exceptionIds).ToArray();
                        SecondPagesParser(ids);
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
                        _log.Error(_currentProxy.ProxyHost + " is bad");
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

        private async void SecondPagesParser(int[] ids)
        {
            if (ids == null || ids.Length == 0)
                throw new Exception("The array of ids is empty");
            var ids10 = ids.Take(10).ToArray();
            Console.WriteLine("ids: {0}", String.Join(", ", ids10));

            var tasks = new List<Task>();
            foreach (var id in ids10)
                tasks.Add(Task.Run(() => GetUrl(id)));


            await Task.WhenAll(tasks);

            List<UrlModel> results = new List<UrlModel>();
            foreach (var task in tasks)
            {
                var result = ((Task<UrlModel>)task).Result;
                results.Add(result);
            }

            Console.WriteLine("ids: {0}", String.Join(", ", results));
        }

        private UrlModel GetUrl(int id)
        {
            try
            {
                HtmlWeb web = new HtmlWeb();

                var url = Constants.NnmClubTopic + "?t=" + Convert.ToString(id);
                var doc = _useProxy
                    ? web.Load(url, _currentProxy.ProxyHost, _currentProxy.ProxyPort, _currentProxy.UserId,
                        _currentProxy.Password)
                    : web.Load(url);

                string title = doc.DocumentNode.SelectSingleNode("//a[@class='maintitle']").InnerText;
                string imgUrl = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']").Attributes["content"]
                    .Value;
                var urlModel = new UrlModel(id, title, imgUrl);
                Console.WriteLine(urlModel);
                return urlModel;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

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