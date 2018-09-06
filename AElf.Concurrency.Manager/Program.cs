using System;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NLog;

namespace AElf.Concurrency.Manager
{
    class Program
    {
        private static ILogger _logger = LogManager.GetCurrentClassLogger();
        
        static void Main(string[] args)
        {
            var confParser = new ConfigParser();
            bool parsed;
            try
            {
                parsed = confParser.Parse(args);
            }
            catch (Exception e)
            {
                _logger.Error(e);
                throw;
            }

            if (!parsed)
                return;
            var managementService = new ManagementService();
            managementService.StartSeedNodes();
            
            var url = "http://127.0.0.1:9099";
            
            var _host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(url)
                .UseStartup<Startup>()
                .Build();

            _host.RunAsync();
            
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) => { await managementService.StopAsync(); };
            managementService.TerminationHandle.Wait();
        }
    }
}