using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        public static Link Empty = new Link() { Url = null };
        public string Url;
        public string Source;
        public LinkType Type;
    }

    public class SpiderNet
    {
        private WebClient client;
        private ConcurrentQueue<Link> _pages;
        private ConcurrentDictionary<String, bool> _visited;

        public SpiderNet()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
        }
        internal void Weave()
        {
            client = new WebClient();
            _pages = new ConcurrentQueue<Link>();
            _visited = new ConcurrentDictionary<string, bool>();
            AddUrl(new Link(){ Url = StartPage, Source="", Type = LinkType.Page });

            RunSpider();
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

        public void RunSpider()
        {
            Link link;
            while (_pages.TryDequeue(out link))
            {
                if (link.Type == LinkType.Page)
                    FetchPage(link);
                else if (link.Type == LinkType.Resource || link.Type == LinkType.External)
                    FetchHead(link);
            }
        }

        private void Log(string message)
        {
            System.Console.WriteLine(message);
        }

        private void LogError(Link link, Exception e = null)
        {
            System.Console.WriteLine("[ERROR] Broken " +
                                         link.Type.ToString() +
                                         " link to " +
                                         link.Url + "\n on page " + link.Source);
        }

        private void FetchHead(Link link)
        {
            try
            {
                Log("[HEAD] " + link.Url);
                System.Net.WebRequest request = System.Net.WebRequest.Create(link.Url);
                request.Method = "HEAD";
                var data = request.GetResponse();

                //closing connection to prevent further timeouts
                var stream = data.GetResponseStream();
                if (stream != null)
                    stream.Close();

                if (data is HttpWebResponse)
                {
                    HttpWebResponse http = (HttpWebResponse)data;
                    if (http.StatusCode == HttpStatusCode.ServiceUnavailable || http.StatusCode == HttpStatusCode.Forbidden || http.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception("Not expected response content type: " + http.StatusCode.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                LogError(link, e);
            }
        }

        private void FetchPage(Link link)
        {
            try
            {
                Log("[GET] " + link.Url);
                string data = client.DownloadString(link.Url);
                MatchCollection matches;

                //link tags
                matches = Regex.Matches(data, "<link.*?href=[\"'](.*?)[\"'].*?>", RegexOptions.IgnoreCase);
                EachMatch(link, matches, LinkType.Resource);

                //scripts
                matches = Regex.Matches(data, "<script.*?src=[\"'](.*?)[\"'].*?>", RegexOptions.IgnoreCase);
                EachMatch(link, matches, LinkType.Resource);

                //images
                matches = Regex.Matches(data, "<img.*?src=[\"'](.*?)[\"'].*?>", RegexOptions.IgnoreCase);
                EachMatch(link, matches, LinkType.Resource);

                //links
                matches = Regex.Matches(data, "<a.*?href=[\"'](.*?)[\"'].*?>", RegexOptions.IgnoreCase);
                EachMatch(link, matches, LinkType.Page);
            }
            catch (Exception e)
            {
                LogError(link, e);
            }
        }

        private void EachMatch(Link link, MatchCollection images, LinkType type)
        {
            foreach (Match image in images)
            {
                Link newUrl = FixUrl(image.Groups[1].ToString(), link.Url, type);
                if (newUrl.Url != null)
                    AddUrl(newUrl);
            }
        }

        private Link FixUrl(string match, string currentUrl, LinkType type = LinkType.Page)
        {
            //remove 
            var index = match.IndexOf("#");
            if (index == 0)
                return Link.Empty; //inner link

            if (index > 0)
                match = match.Substring(0, index);

            if (match.StartsWith("/"))
            {
                match = StartPage + match.Substring(1);
            }
            else if (!match.StartsWith("http://"))
            {
                match = BaseUrl(currentUrl) + match;
            }

            if (!match.StartsWith(StartPage))
                return new Link() {Url = match, Type = LinkType.External, Source = currentUrl};

            if (match.EndsWith(".zip") || match.EndsWith(".chm") || match.EndsWith(".war"))
                type = LinkType.Resource;

            return new Link(){Url = match, Source = currentUrl, Type = type };
        }

        private string BaseUrl(string url)
        {
            int index = url.IndexOf("?");
            if (index >= 0)
                url = url.Substring(0, index);

            index = url.LastIndexOf("/");
            if (index > 0)
                url = url.Substring(0, index + 1);
            else
                url = StartPage;

            return url;
        }

        public string Domain { get; set; }

        private string _startPage;
        public string StartPage
        {
            get { return _startPage; }
            set
            {
                if (!value.StartsWith("http://"))
                    value = "http://" + value;
                if (!value.EndsWith("/"))
                    value = value + "/";

                _startPage = value;
            }
        }
    }
}
