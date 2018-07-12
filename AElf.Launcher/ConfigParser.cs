using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
//using AElf.Kernel.Concurrency.Config;
//using AElf.Kernel.Concurrency.Execution.Config;
//using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.ChainController;
using AElf.Configuration;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Runtime.CSharp;
using CommandLine;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace AElf.Launcher
{
    public class ConfigParser
    {
        public IAElfNetworkConfig NetConfig { get; private set; }
        public ITxPoolConfig TxPoolConfig { get; private set; }
        public IMinerConfig MinerConfig { get; private set; }
        public INodeConfig NodeConfig { get; private set; }
        public IRunnerConfig RunnerConfig { get; private set; }

        public bool Rpc { get; private set; }
        public int RpcPort { get; private set; }
        public string RpcHost { get; private set; }
        public string DataDir { get; private set; }
        public string NodeAccount { get; set; }

        public bool Success { get; private set; }
        public bool IsMiner { get; private set; }
        public Hash Coinbase { get; private set; }
        public bool IsConsensusInfoGenerater { get; private set; }

        public string InitData { get; private set; }

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
                .WithNotParsed((errs) => { Success = false; });

            return Success;
        }

        private void MapOptions(AElfOptions opts)
        {
            Rpc = !opts.NoRpc;
            RpcPort = opts.RpcPort;
            RpcHost = opts.RpcHost;
            NodeAccount = opts.NodeAccount;
            InitData = opts.InitData;

            // Network
            var netConfig = new AElfNetworkConfig();

            if (opts.Bootnodes != null && opts.Bootnodes.Any())
            {
                netConfig.Bootnodes = new List<NodeData>();

                foreach (var strNodeData in opts.Bootnodes)
                {
                    var nd = NodeData.FromString(strNodeData);
                    if (nd == null) continue;
                    nd.IsBootnode = true;
                    netConfig.Bootnodes.Add(nd);
                }
            }
            else
            {
                netConfig.Bootnodes = new List<NodeData>();
            }

            if (opts.PeersDbPath != null)
                netConfig.PeersDbPath = opts.PeersDbPath;

            if (opts.Peers != null)
                netConfig.Peers = opts.Peers.ToList();

            if (opts.Port.HasValue)
                netConfig.Port = opts.Port.Value;

            /*if (!string.IsNullOrEmpty(opts.Host))
                netConfig.Host = opts.Host;*/

            NetConfig = netConfig;

            // Database
            if (!string.IsNullOrWhiteSpace(opts.DBType) || DatabaseConfig.Instance.Type == DatabaseType.KeyValue)
            {
                DatabaseConfig.Instance.Type = DatabaseTypeHelper.GetType(opts.DBType);
            }

            if (!string.IsNullOrWhiteSpace(opts.DBHost))
            {
                DatabaseConfig.Instance.Host = opts.DBHost;
            }

            if (opts.DBPort.HasValue)
            {
                DatabaseConfig.Instance.Port = opts.DBPort.Value;
            }

            DatabaseConfig.Instance.Number = opts.DBNumber;

            // to be miner
            IsMiner = opts.IsMiner;

            if (opts.IsConsensusInfoGenerator)
            {
                IsConsensusInfoGenerater = true;
            }

            if (opts.NewChain)
            {
                IsMiner = true;
                NewChain = true;
            }

            if (IsMiner)
            {
                if (string.IsNullOrEmpty(opts.NodeAccount))
                {
                    throw new Exception("NodeAccount is needed");
                }

                Coinbase = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(NodeAccount));
            }

            MinerConfig = new MinerConfig
            {
                CoinBase = Coinbase,
                TxCount = opts.TxCountLimit
            };

            // tx pool config
            TxPoolConfig = ChainController.TxPoolConfig.Default;
            TxPoolConfig.FeeThreshold = opts.MinimalFee;
            TxPoolConfig.PoolLimitSize = opts.PoolCapacity;

            // node config
            NodeConfig = new NodeConfig
            {
                IsMiner = IsMiner,
                FullNode = true,
                Coinbase = Coinbase
            };

            // Actor
            if (opts.ActorIsCluster.HasValue)
                ActorConfig.Instance.IsCluster = opts.ActorIsCluster.Value;
            if (!string.IsNullOrWhiteSpace(opts.ActorHostName))
                ActorConfig.Instance.HostName = opts.ActorHostName;
            if (opts.ActorPort.HasValue)
                ActorConfig.Instance.Port = opts.ActorPort.Value;
            if (opts.ActorConcurrencyLevel.HasValue)
            {
                ActorConfig.Instance.ConcurrencyLevel = opts.ActorConcurrencyLevel.Value;
            }

            if (opts.IsParallelEnable.HasValue)
            {
                ParallelConfig.Instance.IsParallelEnable = opts.IsParallelEnable.Value;
            }

            NodeConfig.DataDir = string.IsNullOrEmpty(opts.DataDir)
                ? ApplicationHelpers.GetDefaultDataDir()
                : opts.DataDir;

            // runner config
            RunnerConfig = new RunnerConfig
            {
                SdkDir = Path.GetDirectoryName(typeof(AElf.Kernel.Node.MainChainNode).Assembly.Location)
            };

            if (opts.RunnerConfig != null)
            {
                using (var file = File.OpenText(opts.RunnerConfig))
                using (var reader = new JsonTextReader(file))
                {
                    var cfg = (JObject) JToken.ReadFrom(reader);
                    if (cfg.TryGetValue("csharp", out var j))
                    {
                        RunnerConfig = Runtime.CSharp.RunnerConfig.FromJObject((JObject) j);
                    }
                }
            }
        }
    }
}