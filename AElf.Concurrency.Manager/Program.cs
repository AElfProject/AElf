using System;
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
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) => { await managementService.StopAsync(); };
            managementService.TerminationHandle.Wait();
        }
    }
}