﻿using System;
using System.Collections.Generic;
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
        public string Url;
        public int Worker;
        public bool Verbose;
        public TestCommand()
        {
            //use two workers by default
            Worker = 2;
            Verbose = false;

            IsCommand("test");
            HasOption("s|site=", "Url of site to test", v => Url = v);
            HasOption("w|worker=", "number of workers", v => Worker = Int16.Parse(v));
            HasOption("v|verbose=", "number of workers", v => Verbose = v.Equals("yes"));
        }

        public override int Run(string[] remainingArguments)
        {
            var net = new SpiderNest() { Workers = Worker, Verbose = Verbose };
            net.Weave(Url);

            return 0;
        }
    }
}
