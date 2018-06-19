using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common.Application;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.Kernel.TxMemPool;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers;
using CommandLine;
using Google.Protobuf;

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
        public string DataDir { get; private set; }
        public string NodeAccount { get; set; }

        public bool Success { get; private set; }
        public bool IsMiner { get; private set; }
        public Hash Coinbase { get; private set; }
        
        public string InitData { get; private set; }

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
        // public Hash ChainId { get; set; }
        
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
            NodeAccount = opts.NodeAccount;
            InitData = opts.InitData;
            
            // Network
            AElfNetworkConfig netConfig = new AElfNetworkConfig();

            if (opts.Bootnodes != null && opts.Bootnodes.Any())
            {
                netConfig.Bootnodes = new List<NodeData>();
                
                foreach (var strNodeData in opts.Bootnodes)
                {
                    NodeData nd = NodeData.FromString(strNodeData);
                    if (nd != null)
                    {
                        nd.IsBootnode = true;
                        netConfig.Bootnodes.Add(nd);
                    }
                }
            }
            else
            {
                netConfig.Bootnodes = Bootnodes.BootNodes;
            }

            if (opts.PeersDbPath != null)
                netConfig.PeersDbPath = opts.PeersDbPath;
            
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
                    throw new Exception("coinbase is needed");
                }
                Coinbase = ByteString.CopyFromUtf8(opts.CoinBase);
            }
            
            MinerConfig = new MinerConfig
            {
                CoinBase = Coinbase
            };
            
            
            // tx pool config
            TxPoolConfig = Kernel.TxMemPool.TxPoolConfig.Default;
            TxPoolConfig.FeeThreshold = opts.MinimalFee;
            TxPoolConfig.PoolLimitSize = opts.PoolCapacity;
            
            // node config
            NodeConfig = new NodeConfig
            {
                IsMiner = IsMiner,
                FullNode = true,
                Coinbase = Coinbase
            };
            
            NodeConfig.DataDir = string.IsNullOrEmpty(opts.DataDir) ? ApplicationHelpers.GetDefaultDataDir() : opts.DataDir;
        }
    }

}