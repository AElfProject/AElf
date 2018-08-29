using System;
using System.IO;
using System.Linq;
using AElf.Common.Application;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration.Config.Network;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Configuration
{
    public class CommandLineParser
    {
        public void Parse(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(MapOptions);
        }
        
        private void MapOptions(CommandLineOptions opts)
        {
//            Rpc = !opts.NoRpc;
//            RpcPort = opts.RpcPort;
//            RpcHost = opts.RpcHost;
//            NodeAccount = opts.NodeAccount;
//            NodeAccountPassword = opts.NodeAccountPassword;
//            InitData = opts.InitData;
//
//            // Network
//            if (opts.Bootnodes != null && opts.Bootnodes.Any())
//            {
//                NetworkConfig.Instance.Bootnodes = opts.Bootnodes.ToList();
//            }
//
//            if (opts.PeersDbPath != null)
//                NetworkConfig.Instance.PeersDbPath = opts.PeersDbPath;
//
//            if (opts.Peers != null)
//                NetworkConfig.Instance.Peers = opts.Peers.ToList();
//
//            if (opts.Port.HasValue)
//                NetworkConfig.Instance.ListeningPort = opts.Port.Value;
//
//            // Database
//            DatabaseConfig.Instance.Type = DatabaseTypeHelper.GetType(opts.DBType);
//
//            if (!string.IsNullOrWhiteSpace(opts.DBHost))
//            {
//                DatabaseConfig.Instance.Host = opts.DBHost;
//            }
//
//            if (opts.DBPort.HasValue)
//            {
//                DatabaseConfig.Instance.Port = opts.DBPort.Value;
//            }
//
//            DatabaseConfig.Instance.Number = opts.DBNumber;
//
//            // to be miner
//            IsMiner = opts.IsMiner;
//
//            if (opts.IsConsensusInfoGenerator)
//            {
//                IsConsensusInfoGenerater = true;
//            }
//
//            Globals.ConsensusType = opts.ConsensusType;
//            Console.WriteLine($"Using consensus: {opts.ConsensusType}");
//
//            if (opts.ConsensusType == ConsensusType.AElfDPoS)
//            {
//                Globals.AElfDPoSMiningInterval = opts.AElfDPoSMiningInterval;
//                if (opts.IsConsensusInfoGenerator)
//                {
//                    Console.WriteLine($"Mining interval: {Globals.AElfDPoSMiningInterval} ms");
//                }
//            }
//
//            if (opts.ConsensusType == ConsensusType.PoTC)
//            {
//                Globals.BlockProducerNumber = 1;
//                Globals.ExpectedTransanctionCount = opts.ExpectedTxsCount;
//            }
//
//            if (opts.ConsensusType == ConsensusType.SingleNode)
//            {
//                Globals.BlockProducerNumber = 1;
//                Globals.SingleNodeTestMiningInterval = opts.MiningInterval;
//                Console.WriteLine($"Mining interval: {Globals.SingleNodeTestMiningInterval} ms");
//            }
//
//            if (opts.NewChain)
//            {
//                //IsMiner = true;
//                NewChain = true;
//            }
//
//            ChainId = opts.ChainId;
//
//            if (IsMiner)
//            {
//                if (string.IsNullOrEmpty(opts.NodeAccount))
//                {
//                    throw new Exception("NodeAccount is needed");
//                }
//
//                Coinbase = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(NodeAccount));
//            }
//
//            MinerConfig = new MinerConfig
//            {
//                CoinBase = Coinbase
//            };
//
//            // tx pool config
//            TxPoolConfig = ChainController.TxMemPool.TxPoolConfig.Default;
//            TxPoolConfig.FeeThreshold = opts.MinimalFee;
//            TxPoolConfig.PoolLimitSize = opts.PoolCapacity;
//            TxPoolConfig.Maximal = opts.TxCountLimit;
//
//            // node config
//            NodeConfig.Instance.IsMiner = IsMiner;
//            NodeConfig.Instance.FullNode = true;
//            NodeConfig.Instance.ExecutorType = opts.ExecutorType;
//
//            // Actor
//            if (opts.ActorIsCluster.HasValue)
//                ActorConfig.Instance.IsCluster = opts.ActorIsCluster.Value;
//            if (!string.IsNullOrWhiteSpace(opts.ActorHostName))
//                ActorConfig.Instance.HostName = opts.ActorHostName;
//            if (opts.ActorPort.HasValue)
//                ActorConfig.Instance.Port = opts.ActorPort.Value;
//            if (opts.ActorConcurrencyLevel.HasValue)
//            {
//                ActorConfig.Instance.ConcurrencyLevel = opts.ActorConcurrencyLevel.Value;
//            }
//
//            if (opts.IsParallelEnable.HasValue)
//            {
//                ParallelConfig.Instance.IsParallelEnable = opts.IsParallelEnable.Value;
//            }
//
//            if (opts.Benchmark.HasValue)
//            {
//                ActorConfig.Instance.Benchmark = opts.Benchmark.Value;
//            }
//
//            NodeConfig.Instance.DataDir = string.IsNullOrEmpty(opts.DataDir)
//                ? ApplicationHelpers.GetDefaultDataDir()
//                : opts.DataDir;
//
//            // management config
//            if (!string.IsNullOrWhiteSpace(opts.ManagementUrl))
//            {
//                ManagementConfig.Instance.Url = opts.ManagementUrl;
//            }
//            if (!string.IsNullOrWhiteSpace(opts.ManagementSideChainServicePath))
//            {
//                ManagementConfig.Instance.SideChainServicePath = opts.ManagementSideChainServicePath;
//            }
//
//            
//            // runner config
//            RunnerConfig = new RunnerConfig
//            {
//                SdkDir = Path.GetDirectoryName(typeof(Node.Node).Assembly.Location)
//            };
//
//            if (opts.RunnerConfig != null)
//            {
//                using (var file = File.OpenText(opts.RunnerConfig))
//                using (var reader = new JsonTextReader(file))
//                {
//                    var cfg = (JObject) JToken.ReadFrom(reader);
//                    if (cfg.TryGetValue("csharp", out var j))
//                    {
//                        RunnerConfig = Runtime.CSharp.RunnerConfig.FromJObject((JObject) j);
//                    }
//                }
//            }
        }
    }
}