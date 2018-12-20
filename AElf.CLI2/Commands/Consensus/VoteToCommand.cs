using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands.Consensus
{
    [Verb("vote_to", HelpText = "Vote to your favorite candidate.")]
    public class VoteToOption : BaseOption
    {
        [Value(0, HelpText = "The public key of your favorite candidate.", Required = true)]
        public string PublickKey { get; set; } = "";
    } 
    public class VoteToCommand : Command
    {
        private readonly VoteToOption _option;
        public VoteToCommand(VoteToOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.proposal.js"));
            _engine.GlobalObject.CallMethod("vote_to", _option.PublickKey);
        }
    }
}