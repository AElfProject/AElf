using System;
using Microsoft.AspNetCore.Hosting;

namespace AElf.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "http://0.0.0.0:9099";
            
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}