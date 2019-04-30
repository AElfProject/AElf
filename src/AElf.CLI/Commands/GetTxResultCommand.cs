using System;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("get-tx-result", HelpText = "Get a transaction result.")]
    public class GetTxResultOption : BaseOption
    {
        [Value(0, HelpText = "The tx hash to query.", Required = true)]
        public string TxHash { get; set; } = "";
    }

    public class GetTxResultCommand : Command
    {
        private GetTxResultOption _option;

        public GetTxResultCommand(GetTxResultOption option) : base(option)
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

            _option.TxHash = _option.TxHash.Replace("0x", "");
            if (string.IsNullOrEmpty(_option.TxHash) || _option.TxHash.Length != 64)
            {
                Colors.WriteLine("Provided tx hash is not valid.".DarkRed());
                return;
            }

            try
            {
                // Get res
                _engine.RunScript($@"
                    var res = aelf.chain.getTxResult('{_option.TxHash}');
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