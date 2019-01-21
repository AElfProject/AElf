using System;
namespace AElf.Concurrency.Lighthouse
{
    class Program
    {
        //private static ILogger Logger= LogManager.GetCurrentClassLogger();
        
        //TODO: change using aspnet core configuration
        static void Main(string[] args)
        {
            var confParser = new ConfigParser();
            bool parsed;
            try
            {
                parsed = confParser.Parse(args);
            }
            catch (Exception)
            {
                //Logger.LogError(e);
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