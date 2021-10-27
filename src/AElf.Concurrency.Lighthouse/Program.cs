using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;

namespace AElf.Concurrency.Lighthouse
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var application = AbpApplicationFactory.Create<LighthouseConcurrencyAElfModule>(options =>
            {
                options.UseAutofac();
            }))
            {
                application.Initialize();

                var managementService = application.ServiceProvider.GetRequiredService<ManagementService>();
                managementService.StartSeedNodes();
                Console.WriteLine("Press Control + C to terminate.");
                Console.CancelKeyPress += async (sender, eventArgs) => { await managementService.StopAsync(); };
                managementService.TerminationHandle.Wait();
            }
        }
    }
}