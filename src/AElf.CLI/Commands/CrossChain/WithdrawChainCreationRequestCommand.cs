using System.IO;
using CommandLine;

namespace AElf.CLI.Commands.CrossChain
{
    [Verb("withdraw-sidechain-creation", HelpText = "Withdraw chain creation request..")]
    public class WithdrawChainCreationRequestOption : BaseOption
    {
        [Value(0, HelpText = "Chain id.", Required = true)]
        public string ChainId { get; set; }
    }

    public class WithdrawChainCreationRequestCommand : Command
    {
        private WithdrawChainCreationRequestOption _option;

        public WithdrawChainCreationRequestCommand(WithdrawChainCreationRequestOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "cross-chain.js")));
            _engine.GlobalObject.CallMethod("withdraw_chain_creation_request", _option.ChainId);
        }
    }
}