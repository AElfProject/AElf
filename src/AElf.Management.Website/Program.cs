using System.Timers;
using AElf.Management.Interfaces;
using Autofac;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace AElf.Management.Website
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://0.0.0.0:9090");
    }
}