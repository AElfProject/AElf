using System.Reflection;
using AElf.CLI.JS;
using CommandLine;

namespace AElf.CLI.Commands.CrossChain
{
    [Verb("recharge-sidechain", HelpText = "recharge for side chain.")]
    public class RechargeForSideChainOption : BaseOption
    {
        [Value(0, HelpText = "Chain id.", Required = true)]
        public string ChainId { get; set; }
        
        [Value(1, HelpText = "Amount", Required = true)]
        public int Amount { get; set; } 
    }
    
    public class RechargeForSideChainCommand : Command
    {
        private RechargeForSideChainOption _option;
        public RechargeForSideChainCommand(RechargeForSideChainOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI.Scripts.cross-chain.js"));
            _engine.GlobalObject.CallMethod("recharge_sidechain", _option.ChainId, _option.Amount);
        }
    }
}