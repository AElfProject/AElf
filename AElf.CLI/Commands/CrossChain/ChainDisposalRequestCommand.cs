using System.Reflection;
using AElf.CLI.JS;
using CommandLine;

namespace AElf.CLI.Commands.CrossChain
{
    [Verb("request-sidechain-disposal", HelpText = "Request to dispose sidechain.")]
    public class ChainDisposalRequestOption : BaseOption
    {
        [Value(0, HelpText = "Chain id.", Required = true)]
        public string ChainId { get; set; }
    }
    
    public class ChainDisposalRequestCommand : Command
    {
        private readonly ChainDisposalRequestOption _option;
        public ChainDisposalRequestCommand(ChainDisposalRequestOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI.Scripts.cross-chain.js"));
            _engine.GlobalObject.CallMethod("request_chain_disposal", _option.ChainId);
        }
    }
}