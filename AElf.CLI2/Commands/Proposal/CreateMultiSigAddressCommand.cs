using System;
using System.IO;
using System.Reflection;
using AElf.CLI2.JS;
using AElf.CLI2.Utils;
using Alba.CsConsoleFormat.Fluent;
using CommandLine;

namespace AElf.CLI2.Commands.Proposal
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
            _engine.RunScript(Assembly.LoadFrom(Assembly.GetAssembly(typeof(JSEngine)).Location)
                .GetManifestResourceStream("AElf.CLI2.Scripts.proposal.js"));
            _engine.GlobalObject.CallMethod<int, int, string>("create_multi_sig_account", _option.DecidedWeightThreshold,
                _option.ProposerThreshold, _option.Params);
        }


        /*public override void Execute()
        {
            if (string.IsNullOrEmpty(_option.Endpoint))
            {
                Colors.WriteLine("Endpoint is not provided. Cannot proceed.".DarkRed());
                return;
            }

            if (string.IsNullOrEmpty(_option.Account))
            {
                Colors.WriteLine("Account is not provided. Cannot proceed.".DarkRed());
                return;
            }
            
            InitChain();
            try
            {
                // Get contract and method
                _engine.RunScript($@"
                    var contract = aelf.chain.contractAt('{_option.Contract}', _account);
                    var method = contract['{_option.Method}'];
                ");

                // Prepare arguments
                _engine.RunScript($@"
                    var methodargs = JSON.parse('{_option.Params}');
                ");
                _engine.RunScript($@"
                    var methodAbi = contract.abi.Methods.find(x => x.Name === '{_option.Method}');
                ");
                var inputCount = _engine.Evaluate("methodargs").ReadProperty<int>("length");
                var reqCount = _engine.Evaluate("methodAbi.Params").ReadProperty<int>("length");
                if (inputCount != reqCount)
                {
                    Colors.WriteLine(
                        $@"Method ""{_option.Method}"" on contract ""{_option.Contract}"" requires {reqCount} input arguments."
                            .DarkRed());
                    return;
                }

                // Execute
                _engine.Execute(@"method.apply(null, methodargs);");
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }#1#
        }*/
    }
}