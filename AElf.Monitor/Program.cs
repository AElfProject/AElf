using System;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Configuration.Config.RPC;
using Microsoft.AspNetCore.Hosting;

namespace AElf.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var parsed = new CommandLineParser();
            parsed.Parse(args);

            var handler = new AElfModuleHandler();
            handler.Register(new AkkaModule());
            handler.Build();

            var url = $"http://{RpcConfig.Instance.Host}:{RpcConfig.Instance.Port}";

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}