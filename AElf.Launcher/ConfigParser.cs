using System.Linq;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.TxMemPool;
using CommandLine;

namespace AElf.Launcher
{
    public class ConfigParser
    {
        public IAElfNetworkConfig NetConfig { get; private set; }
        public IAElfServerConfig ServerConfig { get; private set; }
        
        public ITxPoolConfig TxPoolConfig { get; private set; }

        public bool Success { get; private set; } = false;
        
        public bool Parse(string[] args)
        {
            Parser.Default.ParseArguments<AElfOptions>(args)
                .WithParsed(opts =>
                {
                    MapOptions(opts);
                    Success = true;
                })
                .WithNotParsed((errs) =>
                {
                    Success = false;
                });

            return Success;
        }

        private void MapOptions(AElfOptions opts)
        {
            // Network
            AElfNetworkConfig config = new AElfNetworkConfig();

            if (opts.Bootnodes != null && opts.Bootnodes.Any())
                config.Bootnodes = opts.Bootnodes.ToList();

            NetConfig = config;
            
            // Todo ITxPoolConfig
            // Todo IAElfServerConfig
        }
    }
}