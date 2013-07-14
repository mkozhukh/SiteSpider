using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SiteSpider
{
    public enum LinkType
    {
        Page,
        Resource,
        External,
        ExternalNoFollow
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
        private int _busyWorkers;
        private CancellationTokenSource _token = new CancellationTokenSource();
        
        public SpiderNest()
        {
            Workers = 2;
            _busyWorkers = 0;
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

            for (var i = 0; i < Workers; i++)
            {
                Task.Factory.StartNew(() =>
                    {
                        var spider = new Spider(this, domain);
                        spider.Weave(_token.Token);
                    }, _token.Token);
            }

            WaitFinish();
        }

        private void WaitFinish()
        {
            while (true)
            {
                if (_busyWorkers < 1)
                {
                    _token.Cancel();
                    return;
                }
                Thread.Sleep(500);
            }
        }

        public bool GetUrl(out Link result)
        {
            return _pages.TryDequeue(out result);
        }

        public void AddUrl(Link link)
        {
            if (IgnoreMask != null && link.Url.Contains(IgnoreMask))
                return;
            if (ReportMask != null && link.Url.Contains(ReportMask))
                LogReport(link);

            bool isExists;
            if (_visited.TryGetValue(link.Url, out isExists))
                if (isExists)
                    return;


            if (_visited.TryAdd(link.Url, true)){
                WorkerBusy();
                _pages.Enqueue(link);
            }
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

        public void LogReport(Link link)
        {
            Console.WriteLine("[REPORT] " + link.Url + "\n on page " + link.Source);
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
        public string IgnoreMask { get; set; }
        public string ReportMask { get; set; }

        public void WorkerFree()
        {
            System.Threading.Interlocked.Decrement(ref _busyWorkers);
        }

        public void WorkerBusy()
        {
            System.Threading.Interlocked.Increment(ref _busyWorkers);
        }
    }
}
