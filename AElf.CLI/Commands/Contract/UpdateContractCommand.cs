using System.IO;
using System.Reflection;
using AElf.CLI.JS;
using Alba.CsConsoleFormat.Fluent;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("update", HelpText = "Update a smart contract.")]
    public class UpdateContractOption : BaseOption
    {
        [Value(0, MetaName = "ContractAddress", HelpText = "The address of the contract to be updated.", Required = true)]
        public string ContractAddress { get; set; }

        [Value(1, MetaName = "CodeFile", HelpText = "The compiled contract code file of the contract to be deployed.",
            Required = true)]
        public string CodeFile { get; set; }
    }

    public class UpdateContractCommand : Command
    {
        private readonly UpdateContractOption _option;

        public UpdateContractCommand(UpdateContractOption option) : base(option)
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
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI.Scripts.contract.js"));
            _engine.GlobalObject.CallMethod<string, string>("updateCommand", _option.ContractAddress,
                GetCode(_option.CodeFile));
        }
    }
}