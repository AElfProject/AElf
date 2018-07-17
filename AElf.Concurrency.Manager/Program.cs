using System;

namespace AElf.Concurrency.Manager
{
    class Program
    {
        static void Main(string[] args)
        {
            var managementService = new ManagementService();
            managementService.StartSeedNodes();
            Console.WriteLine("Press Control + C to terminate.");
            Console.CancelKeyPress += async (sender, eventArgs) => { await managementService.StopAsync(); };
            managementService.TerminationHandle.Wait();
        }
    }
}