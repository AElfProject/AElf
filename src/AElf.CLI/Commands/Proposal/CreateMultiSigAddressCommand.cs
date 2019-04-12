using System.IO;
using CommandLine;

namespace AElf.CLI.Commands.Proposal
{
    [Verb("create-multi-sig", HelpText = "Create a new multi sig address.")]
    public class CreateMultiSigOption : BaseOption
    {
        [Value(0, HelpText = "Weight threshold for release.", Required = true)]
        public int DecidedWeightThreshold { get; set; }

        [Value(1, HelpText = "Weight threshold for proposer.", Required = true)]
        public int ProposerThreshold { get; set; }

        [Value(2, HelpText = "Authority information containing public key and weight in json array format.", Required = true)]
        public string Params { get; set; } = "";
    }

    public class CreateMultiSigAddressCommand : Command
    {
        private readonly CreateMultiSigOption _option;

        public CreateMultiSigAddressCommand(CreateMultiSigOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "proposal.js")));
            _engine.GlobalObject.CallMethod<int, int, string>("create_multi_sig_account", _option.DecidedWeightThreshold, _option.ProposerThreshold, _option.Params);
        }
    }
}