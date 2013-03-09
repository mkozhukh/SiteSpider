using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SiteSpider
{
    public enum LinkType
    {
        Page,
        Resource,
        External
    }

    public struct Link
    {
        public static Link Empty = new Link { Url = null };
        public string Url;
        public string Source;
        public LinkType Type;
    }

    public class SpiderNest
    {
        private ConcurrentQueue<Link> _pages;
        private ConcurrentDictionary<String, bool> _visited;

        public SpiderNest()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
        }
        internal void Weave(string domain, string startPage = null)
        {

            domain = fixStartPage(domain);
            if (startPage != null)
                startPage = fixStartPage(startPage);
            else
                startPage = domain;

            _pages = new ConcurrentQueue<Link>();
            _visited = new ConcurrentDictionary<string, bool>();
            AddUrl(new Link { Url = startPage, Source = "", Type = LinkType.Page });

            var spider = new Spider(this, domain);
            spider.Weave();
        }

        public bool GetUrl(out Link result)
        {
            return _pages.TryDequeue(out result);
        }
        
        public void AddUrl(Link link)
        {
            bool isExists;
            if (_visited.TryGetValue(link.Url, out isExists))
                if (isExists)
                    return;

            _visited.TryAdd(link.Url, true);
            _pages.Enqueue(link);
        }

        private string fixStartPage(string value)
        {
            if (!value.StartsWith("http://"))
                value = "http://" + value;
            if (!value.EndsWith("/"))
                value = value + "/";

            return value;
        }


        public void Log(string message)
        {
            if (Verbose)
                Console.WriteLine(message);
        }

        public void LogError(Link link, Exception e = null)
        {
            Console.WriteLine("[ERROR] Broken " +
                              link.Type.ToString() +
                              " link to " +
                              link.Url + "\n on page " + link.Source);

            if (e != null && Verbose)
                Console.WriteLine("[EXCEPTION] " + e);
        }

        public int Workers { get; set; }
        public bool Verbose { get; set; }
    }
}
