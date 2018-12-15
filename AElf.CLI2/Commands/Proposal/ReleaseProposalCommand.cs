using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands.Proposal
{
    [Verb("release", HelpText = "Release proposal to execute.")]
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
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.proposal.js"));
            _engine.GlobalObject.CallMethod("release", _option.ProposalHash);
        }
    }
}