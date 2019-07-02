using System;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("get-chain-status", HelpText = "Get the chain status.")]
    public class GetChainStatusOption : BaseOption
    {
    }
    public class GetChainStatusCammand: Command
    {
        private GetChainStatusOption _option;
        public GetChainStatusCammand(GetChainStatusOption option) : base(option)
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
                // Get res
                _engine.RunScript($@"
                    var res = aelf.chain.getChainStatus();
                ");
                // Format res
                _engine.RunScript($@"
                    var resStr = JSON.stringify(res, null, 2);
                ");
                Console.WriteLine(_engine.GlobalObject.ReadProperty<string>("resStr"));
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }
        }
    }
}