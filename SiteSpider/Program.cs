using System;
using System.Collections.Generic;
using System.Text;
using ManyConsole;

namespace SiteSpider
{
    class Program
    {
        static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;

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
        public string Url;
        public int Worker;
        public bool Verbose;
        public string IgnoreMask;
        public string ReportMask;
        public TestCommand()
        {
            //use two workers by default
            Worker = 2;
            Verbose = false;
            
            IsCommand("test");
            HasOption("s|site=", "Url of site to test", v => Url = v);
            HasOption("w|worker=", "number of workers", v => Worker = Int16.Parse(v));
            HasOption("v|verbose=", "number of workers", v => Verbose = v.Equals("yes"));
            HasOption("i|ignore=", "ignore urls with defined mask", v => IgnoreMask = v);
            HasOption("r|report=", "report urls with defined mask", v => ReportMask = v);
        }

        public override int Run(string[] remainingArguments)
        {
            if (Url != null)
            {
                var reports = ReportMask != null ? ReportMask.Split(',') : null;
                var ignores = IgnoreMask != null ? IgnoreMask.Split(',') : null;
                var net = new SpiderNest { Workers = Worker, Verbose = Verbose, IgnoreMask = ignores, ReportMask = reports };
                net.Weave(Url);
            }

            return 0;
        }
    }
}
