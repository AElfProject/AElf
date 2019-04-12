using System.IO;
using System.Threading.Tasks;
using AElf.Blockchains.MainChain;
using AElf.Kernel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            var builder = new WebHostBuilder()
                .ConfigureAppConfiguration(config => { config.AddJsonFile("appsettings.json"); })
                .UseKestrel((builderContext, options) =>
                {
                    options.Configure(builderContext.Configuration.GetSection("Kestrel"));
                })
                .ConfigureLogging(logger => { })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<MainBlockchainStartup<MainChainAElfModule>>()
                .ConfigureServices(services =>
                {
                    services.Configure<ChainOptions>(o =>
                        o.ChainId = blockchainApplicationStartDto.ChainId);
                });
            await builder.Build().RunAsync();
        }
    }
}