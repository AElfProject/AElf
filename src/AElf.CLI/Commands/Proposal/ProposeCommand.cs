using System.IO;
using CommandLine;

namespace AElf.CLI.Commands.Proposal
{
    [Verb("create-proposal", HelpText = "Create and propose a proposal.")]
    public class ProposalOption : BaseOption
    {
        [Value(0, HelpText = "Proposal Name.", Required = true)]
        public string ProposalName { get; set; }

        [Value(1, HelpText = "Multi sig address of this proposal.", Required = true)]
        public string MultiSigAccount { get; set; }

        [Value(2, HelpText = "Expired time in second.", Required = true)]
        public int ExpiredTime { get; set; }

        [Value(3, HelpText = "Target address.", Required = true)]
        public string To { get; set; } = "";

        [Value(4, HelpText = "Method name.", Required = true)]
        public string Method { get; set; } = "";

        [Value(5, HelpText = "Packed txn params in proposal.", Required = true)]
        public string Params { get; set; } = "";
    }

    public class ProposeCommand : Command
    {
        private readonly ProposalOption _option;

        public ProposeCommand(ProposalOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "proposal.js")));
            _engine.GlobalObject.CallMethod("propose", _option.ProposalName, _option.MultiSigAccount,
                _option.ExpiredTime, _option.To, _option.Method, _option.Params, _baseOption.Account);
        }
    }
}