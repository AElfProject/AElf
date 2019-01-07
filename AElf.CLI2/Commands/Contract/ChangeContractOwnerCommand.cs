using System.Reflection;
using AElf.CLI2.JS;
using CommandLine;

namespace AElf.CLI2.Commands
{
    [Verb("change-owner", HelpText = "Update a smart contract.")]
    public class ChangeContractOwnerOption : BaseOption
    {
        [Value(0, MetaName = "ContractAddress", HelpText = "The address of the contract to be updated.", Required = true)]
        public string ContractAddress { get; set; }

        [Value(1, MetaName = "NewOwner", HelpText = "The new owner of the contract.", Required = true)]
        public string NewOwner { get; set; }
    }

    public class ChangeContractOwnerCommand : Command
    {
        private readonly ChangeContractOwnerOption _option;

        public ChangeContractOwnerCommand(ChangeContractOwnerOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            InitChain();
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.contract.js"));
            _engine.GlobalObject.CallMethod<string, string>("changeOwnerCommand", _option.ContractAddress,
                _option.NewOwner);
        }
    }
}