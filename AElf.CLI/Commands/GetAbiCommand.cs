using System;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;
using CSharpx;

namespace AElf.CLI.Commands
{
    [Verb("get-abi", HelpText = "Get the abi for a contract or contract method.")]
    public class GetAbiOption : BaseOption
    {
        [Value(0, HelpText = "The address of the contract.", Required = true)]
        public string Contract { get; set; } = "";

        [Value(1, HelpText = "The particular method of the contract.")]
        public string Method { get; set; } = "";
    }

    public class GetAbiCommand : Command
    {
        private readonly GetAbiOption _option;

        public GetAbiCommand(GetAbiOption option) : base(option)
        {
            _option = option;
        }

        public override void Execute()
        {
            if (string.IsNullOrEmpty(_option.Endpoint))
            {
                Colors.WriteLine("Endpoint is not provided. Cannot proceed.".DarkRed());
                return;
            }

            try
            {
                // Get abi
                _engine.RunScript($@"
                    var abi = aelf.chain.getContractAbi('{_option.Contract}');
                ");
                // Format abi
                if (string.IsNullOrEmpty(_option.Method))
                {
                    // For contract
                    _engine.RunScript($@"
                        var abiStr = JSON.stringify(abi, null, 2);
                    ");
                }
                else
                {
                    // For method
                    _engine.RunScript($@"
                        var methodAbi = abi.Methods.find(x => x.Name === '{_option.Method}');
                        var abiStr = JSON.stringify(methodAbi, null, 2);
                    ");
                }

                Console.WriteLine(_engine.GlobalObject.ReadProperty<string>("abiStr"));
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }
        }
    }
}