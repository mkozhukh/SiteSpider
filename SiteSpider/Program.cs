using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ManyConsole;

namespace SiteSpider
{
    class Program
    {
        static int Main(string[] args)
        {
            // locate any commands in the assembly (or use an IoC container, or whatever source)
            var commands = GetCommands();

            // then run them.
            return ConsoleCommandDispatcher.DispatchCommand(commands, args, Console.Out);
        }

        public static IEnumerable<ConsoleCommand> GetCommands()
        {
            return ConsoleCommandDispatcher.FindCommandsInSameAssemblyAs(typeof(Program));
        }
    }

    public class TestCommand: ConsoleCommand
    {
        public string url;
        public TestCommand()
        {
            IsCommand("test");
            HasOption("s|site=", "url of site to test", v => url = v);
        }

        public override int Run(string[] remainingArguments)
        {
            var net = new SpiderNet(){ Domain = url, StartPage = url };
            net.Weave();

            return 0;
        }
    }

    public class SpiderNet
    {
        private ConcurrentQueue<string> _pages;
        private ConcurrentDictionary<String, bool> _visited;

        internal void Weave()
        {
            _pages = new ConcurrentQueue<string>();
            _visited = new ConcurrentDictionary<string, bool>();
            AddUrl(StartPage);

            RunSpider();
        }

        public void AddUrl(string url)
        {
            bool isExists;
            if (_visited.TryGetValue(url, out isExists))
                if (isExists)
                    return;

            _visited.TryAdd(url, true);    
            _pages.Enqueue(url);
        }

        public void RunSpider()
        {
            string url;
            WebClient client = new WebClient();
            while (_pages.TryDequeue(out url))
            {
                System.Console.WriteLine("Downloading: "+url);
                try
                {
                    string data = client.DownloadString(url);
                

                    var matches = Regex.Matches(data, "<a.*href=[\"'](.*?)[\"'].*>", RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        string newUrl = FixUrl(match.Groups[1].ToString(), url);
                        if (newUrl!= null)
                            AddUrl(newUrl);
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("[ERROR] "+url);
                }
            }

            System.Console.Read();
            System.Console.WriteLine("Done");
        }

        private string FixUrl(string match, string currentUrl)
        {
            //remove 
            var index = match.IndexOf("#");
            if (index == 0)
                return null; //inner link

            if (index > 0)
                match = match.Substring(0, index);
            
            if (match.StartsWith("/"))
            {
                match = StartPage + match.Substring(1);
            } else if (!match.StartsWith("http://"))
            {
                match = BaseUrl(currentUrl) + match;
            }

            if (!match.StartsWith(StartPage))
                return null; //different domain

            if (match.EndsWith(".zip") || match.EndsWith(".png") || match.EndsWith(".jpg"))
                return null; //non html files

            return match;
        }

        private string BaseUrl(string url)
        {
            int index = url.IndexOf("?");
            if (index >= 0)
                url = url.Substring(0, index);

            index = url.LastIndexOf("/");
            if (index > 0)
                url = url.Substring(0, index+1);
            else 
                url = StartPage;

            return url;
        }

        public string Domain { get; set; }

        private string _startPage;
        public string StartPage {
            get { return _startPage; }
            set
            {
                if (!value.StartsWith("http://"))
                    value = "http://"+value;
                if (!value.EndsWith("/"))
                    value = value + "/";

                _startPage = value;
            } 
        }
    }
}
