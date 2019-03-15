using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace AElf.Cluster.Application
{

    public class BlockchainApplicationStartDto
    {
        public int ChainId { get; set; }
    }
    
    public class BlockchainApplicationHostingService
    {
        public async Task StartAsync(BlockchainApplicationStartDto blockchainApplicationStartDto)
        {
            new WebHostBuilder()
                .ConfigureAppConfiguration(config =>
                {
                    config.AddJsonFile("appsettings.json");
                })
                .UseKestrel((builderContext, options) =>
                {
                    options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                })
                .ConfigureLogging(logger =>
                {
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<MainBlockchainStartup<MainBlockchainAElfModule>>()
                .ConfigureServices(services => { })
                .Build().RunAsync();
        }
    }
}