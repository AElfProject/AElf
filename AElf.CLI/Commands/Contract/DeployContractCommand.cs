using System.IO;
using Alba.CsConsoleFormat.Fluent;
using CommandLine;

namespace AElf.CLI.Commands.Contract
{
    [Verb("deploy", HelpText = "Deploy a smart contract.")]
    public class DeployContractOption : BaseOption
    {
        [Value(0, MetaName = "Category", HelpText = "The category of the contract to be deployed.", Required = true)]
        public int Category { get; set; }

        [Value(1, MetaName = "CodeFile", HelpText = "The compiled contract code file of the contract to be deployed.",
            Required = true)]
        public string CodeFile { get; set; }
    }

    public class DeployContractCommand : Command
    {
        private readonly DeployContractOption _option;

        public DeployContractCommand(DeployContractOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            if (!File.Exists(_option.CodeFile))
            {
                Colors.WriteLine($@"Code file ""{_option.CodeFile}"" doesn't exist.".DarkRed());
            }

            _engine.RunScript(File.ReadAllText(Path.Combine(_engine.DefaultScriptsPath, "contract.js")));
            _engine.GlobalObject.CallMethod<int, string>("deployCommand", _option.Category, GetCode(_option.CodeFile));
        }
    }
}