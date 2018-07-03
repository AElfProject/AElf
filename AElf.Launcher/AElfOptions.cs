using System.Collections.Generic;
using CommandLine;

namespace AElf.Launcher
{
    public class AElfOptions
    {
        #region General
        
        [Option(HelpText = "The directory the node uses to store data.")]
        public string DataDir { get; set; }
        
        [Option(HelpText = "The key used by the node.")]
        public string NodeAccount { get; set; }
        
        [Option('n', Default = false, HelpText = "Create a new chain if true")]
        public bool NewChain { get; set; }
        
        [Option('m', Default = false, HelpText = "To be a miner verification needed ")]
        public bool IsMiner { get; set; }
        
        [Option("coinbase", HelpText = "Miner coinbase when a new chain created")]
        public string CoinBase { get; set; }
        
        [Option("debug.initfile", HelpText = "Debug: initial chain to load")]
        public string InitData { get; set; }
        
        [Option("tcl", Default = (ulong) 1024, HelpText  = "Transaction count limit in one block")]
        public ulong TxCountLimit { get; set; }

        [Option('g', Default = false, HelpText = "Is the one who will generate DPoS information")]
        public bool IsConsensusInfoGenerator { get; set; }

        /*[Option("ts", Default = (uint)1024, HelpText = "Transaction size limit")]
        public uint TxSizeLimit { get; set; }*/
        
        /*[Option("tx", Default = (ulong) 1024, HelpText = "Maximal transaction count in block")]
        public ulong TxCount { get; set; }*/
        
        /*[Option('m', Default = 'l', HelpText = "Full node mode for a new chain if mode is \'f\', otherwise light node mode for main chain.")]
         public char Mode { get; set; }*/
        
        #endregion
        
        #region Transaction Pool
        
        [Option("pc", Default = (ulong)4096, HelpText  = "Transaction pool capacity limit")]
        public ulong PoolCapacity { get; set; }
        
        [Option("fee", Default = (ulong)0, HelpText = "Minimal fee for entry into pool")]
        public ulong MinimalFee { get; set; }
        
        #endregion
        
        #region Network and RPC

        [Option('b', HelpText = "Replaces the bootnode list.")]
        public IEnumerable<string> Bootnodes { get; set; }
        
        [Option(HelpText = "Sets an initial list of peers. Format: IP:Port")]
        public IEnumerable<string> Peers { get; set; }
        
        [Option(HelpText = "The IP address this node is listening on.")]
        public string Host { get; set; }
        
        [Option(HelpText = "The port this node is listening on.")]
        public int? Port { get; set; }
        
        [Option(Default = false, HelpText = "Starts the node without exposing the RPC interface.")]
        public bool NoRpc { get; set; }
        
        [Option("rpc.port", Default = 5000, HelpText = "The port that the RPC server.")]
        public int RpcPort { get; set; }
        
        [Option(HelpText = "The absolute path where to store the peer database.")]
        public string PeersDbPath { get; set; }

        #endregion
        
        #region Database
        
        [Option('t', HelpText = "The type of database.")]
        public string DBType { get; set; }

        [Option('h', HelpText = "The IP address of database.")]
        public string DBHost { get; set; }

        [Option('p', HelpText = "The port of database.")]
        public int? DBPort { get; set; }
        
        #endregion
        
        #region Actor
        [Option("actor.iscluster", HelpText = "Actor is cluster or not.")]
        public bool? ActorIsCluster { get; set; }
        
        [Option("actor.host", HelpText = "The hostname of actor.")]
        public string ActorHostName { get; set; }
        
        [Option("actor.port", HelpText = "The port of actor.")]
        public int? ActorPort { get; set; }
        
        [Option("actor.conlevel", Hidden = true, HelpText = "ConcurrencyLevel, used to limit the group count of the result of grouper")]
        public int? ActorConcurrencyLevel { get; set; }

        #endregion

        #region Runner

        [Option("runner.config", HelpText = "The path to the runner config in json format.")]
        public string RunnerConfig { get; set; }

        #endregion
    }
}