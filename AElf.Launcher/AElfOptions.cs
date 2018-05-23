using System.Collections.Generic;
using CommandLine;

namespace AElf.Launcher
{
    public class AElfOptions
    {
        [Option('b', HelpText = "Replaces the bootnode list.")]
        public IEnumerable<string> Bootnodes { get; set; }
        
        [Option(HelpText = "Sets an initial list of peers. Format: IP:Port")]
        public IEnumerable<string> Peers { get; set; }
        
        [Option(HelpText = "The IP address this node is listening on.")]
        public string Host { get; set; }
        
        [Option(HelpText = "The port this node is listening on.")]
        public int? Port { get; set; }
    }
}