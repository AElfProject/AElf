using System;
using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands.CrossChain
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
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.cross-chain.js"));
            _engine.GlobalObject.CallMethod("check_sidechain_status", _option.ChainId);
            
        }
    }
}