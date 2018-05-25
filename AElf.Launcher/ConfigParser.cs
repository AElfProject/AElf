using System.Linq;
using AElf.Kernel.Node.Network;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.TxMemPool;
using CommandLine;

namespace AElf.Launcher
{
    public class ConfigParser
    {
        public IAElfNetworkConfig NetConfig { get; private set; }
        public ITxPoolConfig TxPoolConfig { get; private set; }
        
        public bool Rpc { get; private set; }

        public bool Success { get; private set; }
        
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
            Rpc = !opts.NoRpc;
            
            // Network
            AElfNetworkConfig netConfig = new AElfNetworkConfig();

            if (opts.Bootnodes != null && opts.Bootnodes.Any())
                netConfig.Bootnodes = opts.Bootnodes.ToList();

            if (opts.Peers != null)
                netConfig.Peers = opts.Peers.ToList();
            
            if (opts.Port.HasValue)
                netConfig.Port = opts.Port.Value;

            if (!string.IsNullOrEmpty(opts.Host))
                netConfig.Host = opts.Host;
            
            NetConfig = netConfig;
            
            // Todo ITxPoolConfig
        }
    }
}