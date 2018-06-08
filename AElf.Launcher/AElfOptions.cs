using System.Collections.Generic;
using CommandLine;

namespace AElf.Launcher
{
    public class AElfOptions
    {
        #region Network and RPC

        [Option('b', HelpText = "Replaces the bootnode list.")]
        public IEnumerable<string> Bootnodes { get; set; }
        
        [Option(HelpText = "Sets an initial list of peers. Format: IP:Port")]
        public IEnumerable<string> Peers { get; set; }
        
        [Option(HelpText = "The IP address this node is listening on.")]
        public string Host { get; set; }
        
        [Option(HelpText = "The port this node is listening on.")]
        public int? Port { get; set; }
        
        [Option(Default = false, HelpText = "Starts the node without exposing the RPC interface")]
        public bool NoRpc { get; set; }
        
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
    }
}