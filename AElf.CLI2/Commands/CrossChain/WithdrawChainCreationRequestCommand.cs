using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands.CrossChain
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
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.cross-chain.js"));
            _engine.GlobalObject.CallMethod("withdraw_chain_creation_request", _option.ChainId);
        }
    }
}