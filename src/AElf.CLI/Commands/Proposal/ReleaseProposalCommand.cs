using System.IO;
using CommandLine;

namespace AElf.CLI.Commands.Proposal
{
    [Verb("release-proposal", HelpText = "Release proposal to execute.")]
    public class ReleaseProposalOption : BaseOption
    {
        [Value(0, HelpText = "Hash of proposal you want to release. ", Required = true)]
        public string ProposalHash { get; set; } = "";
    }
    
    public class ReleaseProposalCommand : Command
    {
        private readonly ReleaseProposalOption _option;
        public ReleaseProposalCommand(ReleaseProposalOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "proposal.js")));
            _engine.GlobalObject.CallMethod("release", _option.ProposalHash);
        }
    }
}