using System.IO;
using CommandLine;

namespace AElf.CLI.Commands.CrossChain
{
    [Verb("check-sidechain-status", HelpText = "Check status of provided chain.")]
    public class CheckChainStatusOption : BaseOption
    {
        [Value(0, HelpText = "Chain id.", Required = true)]
        public string ChainId { get; set; }
    }
    
    public class CheckChainStatusCommand : Command
    {
        private readonly CheckChainStatusOption _option;
        public CheckChainStatusCommand(CheckChainStatusOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "cross-chain.js")));
            _engine.GlobalObject.CallMethod("check_sidechain_status", _option.ChainId);
        }
    }
}