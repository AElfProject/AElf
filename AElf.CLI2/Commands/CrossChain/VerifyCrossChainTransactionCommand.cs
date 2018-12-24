using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands.CrossChain
{
    [Verb("verify-crosschain-transaction", HelpText = "Verify existence of transaction from another chain.")]
    public class VerifyCrossChainTransactionOption : BaseOption
    {
        [Value(0, HelpText = "Transaction hash you want to verify. ", Required = true)]
        public string Txid { get; set; }
        
        [Value(1, HelpText = "Merkle path for verification.", Required = true)]
        public string Merklepath { get; set; }
        
        [Value(2,  HelpText = "Height of parent chain block indexing provided transaction.",
            Required = true)]
        public double ParentHeight { get; set; }
    }
    
    public class VerifyCrossChainTransactionCommand : Command
    {
        private readonly VerifyCrossChainTransactionOption _option;
        public VerifyCrossChainTransactionCommand(VerifyCrossChainTransactionOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.cross-chain.js"));
            _engine.GlobalObject.CallMethod("verify_crosschain_transaction", _option.Txid, _option.Merklepath,
                _option.ParentHeight);
        }
    }
}