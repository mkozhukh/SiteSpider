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
}
