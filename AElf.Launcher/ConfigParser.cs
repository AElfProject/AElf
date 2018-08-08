using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.ChainController;
using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Types;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Runtime.CSharp;
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
        public IMinerConfig MinerConfig { get; private set; }
        public INodeConfig NodeConfig { get; private set; }
        public IRunnerConfig RunnerConfig { get; private set; }

        public bool Rpc { get; private set; }
        public int RpcPort { get; private set; }
        public string RpcHost { get; private set; }
        public string DataDir { get; private set; }
        public string NodeAccount { get; set; }
        public string NodeAccountPassword { get; set; }

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
            NodeAccountPassword = opts.NodeAccountPassword;
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
                    //nd.IsBootnode = true;
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

            NetConfig = netConfig;

            // Database
            if (!string.IsNullOrWhiteSpace(opts.DBType) || DatabaseConfig.Instance.Type == DatabaseType.InMemory)
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

            Globals.ConsensusType = opts.ConsensusType;
            Console.WriteLine($"Using consensus: {opts.ConsensusType}");

            if (opts.ConsensusType == ConsensusType.AElfDPoS)
            {
                Globals.AElfDPoSMiningInterval = opts.AElfDPoSMiningInterval;
                if (opts.IsConsensusInfoGenerator)
                {
                    Console.WriteLine($"Mining interval: {Globals.AElfDPoSMiningInterval} ms");
                }
            }

            if (opts.ConsensusType == ConsensusType.PoTC)
            {
                Globals.BlockProducerNumber = 1;
                Globals.ExpectedTransanctionCount = opts.ExpectedTxsCount;
            }

            if (opts.ConsensusType == ConsensusType.SingleNode)
            {
                Globals.BlockProducerNumber = 1;
                Globals.SingleNodeTestMiningInterval = opts.MiningInterval;
                Console.WriteLine($"Mining interval: {Globals.SingleNodeTestMiningInterval} ms");
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
                CoinBase = Coinbase
            };

            // tx pool config
            TxPoolConfig = ChainController.TxPoolConfig.Default;
            TxPoolConfig.FeeThreshold = opts.MinimalFee;
            TxPoolConfig.PoolLimitSize = opts.PoolCapacity;
            TxPoolConfig.Maximal = opts.TxCountLimit;

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

            if (opts.Benchmark.HasValue)
            {
                ActorConfig.Instance.Benchmark = opts.Benchmark.Value;
            }

            NodeConfig.DataDir = string.IsNullOrEmpty(opts.DataDir)
                ? ApplicationHelpers.GetDefaultDataDir()
                : opts.DataDir;

            // runner config
            RunnerConfig = new RunnerConfig
            {
                SdkDir = Path.GetDirectoryName(typeof(MainChainNode).Assembly.Location)
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