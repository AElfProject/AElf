using System.IO;
using CommandLine;

namespace AElf.CLI.Commands.Proposal
{
    [Verb("approve-proposal", HelpText = "Approve the proposal provided.")]
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
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "proposal.js")));
            _engine.GlobalObject.CallMethod("approve", _option.ProposalHash);
        }
    }
}