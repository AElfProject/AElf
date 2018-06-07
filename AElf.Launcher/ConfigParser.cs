using System;
using System.IO;
using System.Linq;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.TxMemPool;
using CommandLine;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Launcher
{
    public class ConfigParser
    {
        public IAElfNetworkConfig NetConfig { get; private set; }
        public ITxPoolConfig TxPoolConfig { get; private set; }
        public IDatabaseConfig DatabaseConfig { get; private set; }
        public IMinerConfig MinerConfig { get; private set; }
        public INodeConfig NodeConfig { get; private set; }

        public bool Rpc { get; private set; }

        public bool Success { get; private set; }
        public bool IsMiner { get; private set; }

        /// <summary>
        /// fullnode if true, light node if false
        /// </summary>
        //public bool FullNode { get; private set; }
        
        /// <summary>
        /// create new chain if true
        /// </summary>
        public bool NewChain { get; private set; }
        
        
        /// <summary>
        /// chainId
        /// </summary>
        public Hash ChainId { get; set; }
        
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
           
            
            // to be miner
            IsMiner = opts.IsMiner;
            
            
            if (opts.NewChain)
            {
                IsMiner = true;
                NewChain = true;
            }
            
            if (IsMiner)
            {
                if (string.IsNullOrEmpty(opts.CoinBase))
                {
                    
                }
                // full node for private chain
                MinerConfig = new MinerConfig
                {
                    CoinBase = new Hash(ByteString.FromBase64(opts.CoinBase)),
                    TxCount = opts.TxCount
                };
            }
            
            // tx pool config
            TxPoolConfig = new TxPoolConfig
            {
                PoolLimitSize = opts.PoolCapacity,
                TxLimitSize = opts.TxSizeLimit,
                FeeThreshold = opts.MinimalFee,
                ChainId = ChainId
            };
            
            // node config
            NodeConfig = new NodeConfig
            {
                IsMiner = IsMiner,
                FullNode = true,
                ChainId = ChainId
            };

        }
    }

}