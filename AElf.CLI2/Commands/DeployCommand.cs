using System.IO;
using CommandLine;

namespace AElf.CLI2.Commands
{
    [Verb("deploy", HelpText = "Account related options")]
    public class DeployOption : BaseOption
    {
        [Value(0, MetaName = "category", HelpText = "The category of the contract to be deployed.", Required = true)]
        public int Category { get; set; }

        [Value(1, MetaName = "codefile", HelpText = "The compiled contract code file of the contract to be deployed.",
            Required = true)]
        public string codefile { get; set; }

    }

    public class DeployCommand : ICommand
    {
        private BaseOption _option;
        public DeployCommand(BaseOption option)
        {
            _option = option;

        }
        public void Execute()
        {
//            _option.Account;
        }
    }
}