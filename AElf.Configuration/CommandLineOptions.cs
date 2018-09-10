using System.Collections.Generic;
using CommandLine;

namespace AElf.Configuration
{
    public class CommandLineOptions
    {
        #region Chain

        [Option('n', "chain.new", Default = false, HelpText = "Create a new chain if true")]
        public bool NewChain { get; set; }

        [Option("chain.coinbase", HelpText = "Miner coinbase when a new chain created")]
        public string CoinBase { get; set; }

        [Option("chain.id", HelpText = "The ID of new chain")]
        public string ChainId { get; set; }

        #endregion

        #region Node

        [Option("bootnodes", HelpText = "Replaces the bootnode list.")]
        public IEnumerable<string> Bootnodes { get; set; }

        [Option("node.peers", HelpText = "Sets an initial list of peers. Format: IP:Port")]
        public IEnumerable<string> Peers { get; set; }

        [Option("node.port", HelpText = "The port this node is listening on.")]
        public int? Port { get; set; }
        
        [Option("node.name", HelpText = "The name used to describe the node.")]
        public string NodeName { get; set; }

        [Option("node.account", HelpText = "The key used by the node.")]
        public string NodeAccount { get; set; }

        [Option("node.accountpassword", HelpText = "The password of the account key.")]
        public string NodeAccountPassword { get; set; }

        [Option("node.datadir", HelpText = "The directory the node uses to store data.")]
        public string DataDir { get; set; }

        [Option('e', "node.executor", Default = "simple", HelpText = "The type of txn executor. Must be in [simple, akka].")]
        public string ExecutorType { get; set; }

        #endregion

        #region Block

        [Option("block.transactions", Default = (ulong) 1024, HelpText = "Transaction count limit in one block")]
        public ulong TxCountLimit { get; set; }

        #endregion

        #region Transaction

        [Option("txpool.capacity", Default = (ulong) 4096, HelpText = "Transaction pool capacity limit")]
        public ulong PoolCapacity { get; set; }

        [Option("txpool.fee", Default = (ulong) 0, HelpText = "Minimal fee for entry into pool")]
        public ulong MinimalFee { get; set; }

        #endregion

        #region Database

        // TODO: Find a way to generate help text or just use the enum DBType as its type.
        [Option('t', "db.type", HelpText = "The type of database. Must in [InMemory, Redis, SSDB].")]
        public string DBType { get; set; }

        [Option("db.host", HelpText = "The IP address of database.")]
        public string DBHost { get; set; }

        [Option("db.port", HelpText = "The port of database.")]
        public int? DBPort { get; set; }

        [Option("db.number", Default = 0, HelpText = "The number of database.")]
        public int DBNumber { get; set; }

        #endregion

        #region RPC

        [Option("rpc.disable", Default = false, HelpText = "Starts the node without exposing the RPC interface.")]
        public bool NoRpc { get; set; }

        [Option("rpc.port", Default = 5000, HelpText = "The port that the RPC server.")]
        public int RpcPort { get; set; }

        [Option("rpc.host", Default = "127.0.0.1", HelpText = "The port that the RPC server.")]
        public string RpcHost { get; set; }

        [Option(HelpText = "The absolute path where to store the peer database.")]
        public string PeersDbPath { get; set; }

        #endregion

        #region Miner

        [Option('m', "mine.enable", Default = false, HelpText = "To be a miner verification needed ")]
        public bool IsMiner { get; set; }

        #endregion

        #region Consensus

        [Option('g', "dpos.generator", Default = false, HelpText = "Is the one who will generate DPoS information")]
        public bool IsConsensusInfoGenerator { get; set; }

        [Option("consensus.type", Default = "AElfDPoS", HelpText = "Select the consensus type: AElfDPoS,PoTC or SingleNode")]
        public string ConsensusType { get; set; }

        [Option("dpos.interval", Default = 4000, HelpText = "Mining interval of AElf DPoS.")]
        // ReSharper disable once InconsistentNaming
        public int AElfDPoSMiningInterval { get; set; }

        [Option("potc.count", Default = (ulong) 8000, HelpText = "Expected transactions count.")]
        public ulong ExpectedTxsCount { get; set; }

        [Option("single.interval", Default = 4000, HelpText = "Mining interval if use single node to test other logic.")]
        public int MiningInterval { get; set; }

        #endregion

        #region Runner

        [Option("runner.config", HelpText = "The path to the runner config in json format.")]
        public string RunnerConfig { get; set; }

        #endregion

        #region Actor

        [Option("actor.cluster", HelpText = "Actor is cluster or not.")]
        public bool? ActorIsCluster { get; set; }

        [Option("actor.host", HelpText = "The hostname of actor.")]
        public string ActorHostName { get; set; }

        [Option("actor.port", HelpText = "The port of actor.")]
        public int? ActorPort { get; set; }

        //hide the options about concurrency cause the module haven't finished.
        [Option("actor.conlevel", Hidden = true, HelpText = "ConcurrencyLevel, used to limit the group count of the result of grouper")]
        public int? ActorConcurrencyLevel { get; set; }

        [Option("EnableParallel", Hidden = true, HelpText = "Parallel feature is disabled by default due to lack of support of calling other contracts in one contract")]
        public bool? IsParallelEnable { get; set; }

        [Option("actor.benchmark", Hidden = true, HelpText = "")]
        public bool? Benchmark { get; set; }

        #endregion

        #region Debug

        [Option("log.level", HelpText = "Log level: 6=Off, 5=Fatal 4=Error, 3=Warn, 2=Info, 1=Debug, 0=Trace (default: 2)")]
        public int LogLevel { get; set; } = 0;

        #endregion

        #region Management

        [Option("management.url", HelpText = "The url for the management api.")]
        public string ManagementUrl { get; set; }

        [Option("management.sidechainservicepath", Default = "/api/sidechain", HelpText = "The path for the side chain service endpoint.")]
        public string ManagementSideChainServicePath { get; set; }

        #endregion
    }
}