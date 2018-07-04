using System.Collections.Generic;
using CommandLine;

namespace AElf.Launcher
{
    public class AElfOptions
    {
        #region Chain

        [Option("chain.new", Default = false, HelpText = "Create a new chain if true")]
        public bool NewChain { get; set; }

        [Option("chain.coinbase", HelpText = "Miner coinbase when a new chain created")]
        public string CoinBase { get; set; }

        #endregion

        #region Node

        [Option("bootnodes", HelpText = "Replaces the bootnode list.")]
        public IEnumerable<string> Bootnodes { get; set; }

        [Option("node.peers", HelpText = "Sets an initial list of peers. Format: IP:Port")]
        public IEnumerable<string> Peers { get; set; }

        [Option("node.host", HelpText = "The IP address this node is listening on.")]
        public string Host { get; set; }

        [Option("node.port", HelpText = "The port this node is listening on.")]
        public int? Port { get; set; }

        [Option("node.account", HelpText = "The key used by the node.")]
        public string NodeAccount { get; set; }

        [Option("node.datadir", HelpText = "The directory the node uses to store data.")]
        public string DataDir { get; set; }

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

        [Option("db.type", HelpText = "The type of database.")]
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

        [Option("mine.enable", Default = false, HelpText = "To be a miner verification needed ")]
        public bool IsMiner { get; set; }

        #endregion

        #region DPOS

        [Option("dpos.generator", Default = false, HelpText = "Is the one who will generate DPoS information")]
        public bool IsConsensusInfoGenerator { get; set; }

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

        #endregion

        #region Debug

        [Option("debug", HelpText = "Enable debug.")]
        public bool? Debug { get; set; }

        [Option("debug.initfile", HelpText = "Debug: initial chain to load")]
        public string InitData { get; set; }

        #endregion
    }
}