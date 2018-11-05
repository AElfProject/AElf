using System;
using System.Linq;
using AElf.Common;
using AElf.Common.Application;
using AElf.Common.Enums;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.Network;
using AElf.Configuration.Config.RPC;
using CommandLine;
using NLog;

namespace AElf.Configuration
{
    public class CommandLineParser
    {
        public void Parse(string[] args)
        {
            Parser.Default.ParseArguments<CommandLineOptions>(args).WithParsed(MapOptions);
        }

        private void MapOptions(CommandLineOptions opts)
        {
            ApplicationHelpers.SetConfigPath(opts.ConfigPath);
            
            //database
            if (!string.IsNullOrWhiteSpace(opts.DBType))
            {
                DatabaseConfig.Instance.Type = DatabaseTypeHelper.GetType(opts.DBType);
            }
            else
            {
                if (DatabaseConfig.Instance.Type == DatabaseType.InMemory)
                {
                    throw new ArgumentException("If you want to stored data in memory, specify it in the command line!");
                }
            }

            // Rpc
            RpcConfig.Instance.UseRpc = !opts.NoRpc;
            RpcConfig.Instance.Port = opts.RpcPort;
            RpcConfig.Instance.Host = opts.RpcHost;

            // Network
            if (opts.Bootnodes != null && opts.Bootnodes.Any())
                NetworkConfig.Instance.Bootnodes = opts.Bootnodes.ToList();

            if (opts.PeersDbPath != null)
                NetworkConfig.Instance.PeersDbPath = opts.PeersDbPath;

            if (opts.Peers != null)
                NetworkConfig.Instance.Peers = opts.Peers.ToList();

            if (opts.Port.HasValue)
                NetworkConfig.Instance.ListeningPort = opts.Port.Value;

            NetworkConfig.Instance.NetAllowed = opts.NetAllowed;
            NetworkConfig.Instance.NetWhitelist = opts.NetWhitelist.ToList();

            ConsensusConfig.Instance.ConsensusType = ConsensusTypeHelper.GetType(opts.ConsensusType);

            // tx pool config
            TransactionPoolConfig.Instance.FeeThreshold = opts.MinimalFee;
            TransactionPoolConfig.Instance.PoolLimitSize = opts.PoolCapacity;
            TransactionPoolConfig.Instance.Maximal = opts.TxCountLimit;

            // node config
            NodeConfig.Instance.IsMiner = opts.IsMiner;
            NodeConfig.Instance.ExecutorType = opts.ExecutorType;
            NodeConfig.Instance.ChainId = opts.ChainId;
            NodeConfig.Instance.IsChainCreator = opts.NewChain;
            NodeConfig.Instance.NodeName = opts.NodeName;
            NodeConfig.Instance.NodeAccount = opts.NodeAccount;
            NodeConfig.Instance.NodeAccountPassword = opts.NodeAccountPassword;
            NodeConfig.Instance.ConsensusInfoGenerator = opts.IsConsensusInfoGenerator;
            // TODO: 
            NodeConfig.Instance.ConsensusKind = ConsensusKind.AElfDPoS;

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

            // management config
            if (!string.IsNullOrWhiteSpace(opts.ManagementUrl))
            {
                ManagementConfig.Instance.Url = opts.ManagementUrl;
            }

            if (!string.IsNullOrWhiteSpace(opts.ManagementSideChainServicePath))
            {
                ManagementConfig.Instance.SideChainServicePath = opts.ManagementSideChainServicePath;
            }
            
            LogManager.GlobalThreshold = LogLevel.FromOrdinal(opts.LogLevel);
        }
    }
}