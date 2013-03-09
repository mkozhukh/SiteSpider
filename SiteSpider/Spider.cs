using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace SiteSpider
{
    class Spider
    {
        private readonly SpiderNest _nest;
        private readonly WebClient _client;
        private readonly String _domain;
         
        public Spider(SpiderNest nest, string domain)
        {
            _nest = nest;
            _client = new WebClient();
            _domain = domain;
        }

        public void Weave(CancellationToken token)
        {
            Link link;
            
            while(true){
                while (_nest.GetUrl(out link))
                {
                    if (link.Type == LinkType.Page)
                        FetchPage(link);
                    else if (link.Type == LinkType.Resource || link.Type == LinkType.External)
                        FetchHead(link);
                    _nest.WorkerFree();
                }

                Thread.Sleep(500);
                if (token.IsCancellationRequested)
                    break;
            }

        }

        private void FetchHead(Link link)
        {
            try
            {
                _nest.Log("[HEAD] " + link.Url);
                WebRequest request = WebRequest.Create(link.Url);
                request.Method = "HEAD";
                var data = request.GetResponse();

                //closing connection to prevent further timeouts
                var stream = data.GetResponseStream();
                if (stream != null)
                    stream.Close();

                if (data is HttpWebResponse)
                {
                    HttpWebResponse http = (HttpWebResponse) data;
                    if (http.StatusCode == HttpStatusCode.ServiceUnavailable ||
                        http.StatusCode == HttpStatusCode.Forbidden || http.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new Exception("Not expected response content type: " + http.StatusCode.ToString());
                    }
                }
            }
            catch (WebException web)
            {
                if (((HttpWebResponse)(web.Response)).StatusCode != HttpStatusCode.Forbidden || link.Type != LinkType.External)
                    _nest.LogError(link, web);
                web.Response.Dispose();
            }
            catch (Exception e)
            {
                _nest.LogError(link, e);
            }
        }

        private void FetchPage(Link link)
        {
            try
            {
                _nest.Log("[GET] " + link.Url);
                string data = _client.DownloadString(link.Url);
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
                _nest.LogError(link, e);
            }
        }

        private void EachMatch(Link link, MatchCollection images, LinkType type)
        {
            foreach (Match image in images)
            {
                Link newUrl = FixUrl(image.Groups[1].ToString(), link.Url, type);
                if (newUrl.Url != null)
                    _nest.AddUrl(newUrl);
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

            if (match.StartsWith("http://") || match.StartsWith("https://"))
            {
                //do nothing
            }
            else if (match.StartsWith("//"))
            {
                match = "http:" + match;
            }
            else if (match.StartsWith("/"))
            {
                match = _domain + match.Substring(1);
            }
            else
            {
                match = BaseUrl(currentUrl) + match;
            }

            if (!match.StartsWith(_domain))
                return new Link { Url = match, Type = LinkType.External, Source = currentUrl };

            if (match.EndsWith(".zip") || match.EndsWith(".chm") || match.EndsWith(".war"))
                type = LinkType.Resource;

            return new Link { Url = match, Source = currentUrl, Type = type };
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
                url = _domain;

            return url;
        }
    }
}
