using System.IO;
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
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "cross-chain.js")));
            _engine.GlobalObject.CallMethod("recharge_sidechain", _option.ChainId, _option.Amount);
        }
    }
}