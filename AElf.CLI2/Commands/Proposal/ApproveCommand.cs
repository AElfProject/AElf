using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands.Proposal
{
    [Verb("approve-proposal", HelpText = "Get the block info for a block height.")]
    public class ApprovalOption : BaseOption
    {
        [Value(0, HelpText = "Hash of proposal you want to approve. ", Required = true)]
        public string ProposalHash { get; set; } = "";
    } 
    public class ApproveCommand : Command
    {
        private readonly ApprovalOption _option;
        public ApproveCommand(ApprovalOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.proposal.js"));
            _engine.GlobalObject.CallMethod("approve", _option.ProposalHash);
        }
    }
}