using System;
using AElf.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace AElf.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsed = new CommandLineParser();
            parsed.Parse(args);

            /*var handler = new AElfModuleHandler();
            handler.Register(new AkkaModule());
            handler.Build();*/

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}