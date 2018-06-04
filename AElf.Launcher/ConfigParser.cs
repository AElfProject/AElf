using System.Collections.Generic;
using System.Linq;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel.TxMemPool;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers;
using CommandLine;

namespace AElf.Launcher
{
    public class ConfigParser
    {
        public IAElfNetworkConfig NetConfig { get; private set; }
        public ITxPoolConfig TxPoolConfig { get; private set; }
        public IDatabaseConfig DatabaseConfig { get; private set; }

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
            {
                netConfig.Bootnodes = new List<NodeData>();
                
                foreach (var strNodeData in opts.Bootnodes)
                {
                    NodeData nd = NodeData.FromString(strNodeData);
                    if(nd != null)
                        netConfig.Bootnodes.Add(nd);
                }
            }
            else
            {
                netConfig.Bootnodes = Bootnodes.BootNodes;
            }

            if (opts.Peers != null)
                netConfig.Peers = opts.Peers.ToList();
            
            if (opts.Port.HasValue)
                netConfig.Port = opts.Port.Value;

            if (!string.IsNullOrEmpty(opts.Host))
                netConfig.Host = opts.Host;
            
            NetConfig = netConfig;
            
            // Todo ITxPoolConfig
            
            // Database
            var databaseConfig = new DatabaseConfig();
            
            databaseConfig.Type = DatabaseTypeHelper.GetType(opts.DBType);
            
            if (!string.IsNullOrWhiteSpace(opts.DBHost))
            {
                databaseConfig.Host = opts.DBHost;
            }
            
            if (opts.DBPort.HasValue)
            {
                databaseConfig.Port = opts.DBPort.Value;
            }

            DatabaseConfig = databaseConfig;
        }
    }
}