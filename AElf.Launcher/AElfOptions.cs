using System.Collections.Generic;
using CommandLine;

namespace AElf.Launcher
{
    public class AElfOptions
    {
        [Option('b', HelpText = "Replaces the bootnode list.")]
        public IEnumerable<string> Bootnodes { get; set; }
    }
}