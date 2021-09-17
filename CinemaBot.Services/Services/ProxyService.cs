using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CinemaBot.Services.Services
{
    public class ProxyService
    {
        const string ProxyFilename ="proxy.txt";
        private List<Proxy> _proxies;
        
        public ProxyService()
        {
            string proxyFullname = Directory.GetCurrentDirectory() + "\\" + ProxyFilename;
            
            if (!File.Exists(proxyFullname)) {
                throw new Exception($"File {proxyFullname} not found");
            }

            _proxies = new List<Proxy>();
            
            using (StreamReader stream = new StreamReader(proxyFullname, Encoding.Default))
            {
                string line;
                while ((line = stream.ReadLine()) != null)
                {
                    string[] parts = line.Split(':');
                    
                    // Proxy proxy = new Proxy()
                    Console.WriteLine(string.Join(",", parts));
                }
            }
        }
    }
}