using AElf.Kernel.Orleans.Core;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;
using Orleans.Statistics;

var host = new SiloHostBuilder()
    .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(SmartContractGrain).Assembly).WithReferences())
    .UseLocalhostClustering()
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .UseDashboard()
    .UseLinuxEnvironmentStatistics()
    .AddMemoryGrainStorage("AElf")
    .AddSimpleMessageStreamProvider("SMSProvider")
    .UseInMemoryReminderService()
    .Build();

await host.StartAsync();

Console.WriteLine("Press a key to stop the Silo.");
Console.ReadLine();

await host.StopAsync();