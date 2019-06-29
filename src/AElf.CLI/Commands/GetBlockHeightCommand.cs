using System;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("get-blk-height", HelpText = "Get the block height.")]
    public class GetBlockHeightOption : BaseOption
    {
    }

    public class GetBlockHeightCommand : Command
    {
        private GetBlockHeightOption _option;

        public GetBlockHeightCommand(GetBlockHeightOption option) : base(option)
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
                    var res = aelf.chain.getBlockHeight('{"sync:true"}');
                ");
                // Format res
                _engine.RunScript($@"
                    var resStr = JSON.stringify(res, null, 2);
                ");
                var res = _engine.GlobalObject.ReadProperty<string>("resStr");
                Console.WriteLine(_engine.GlobalObject.ReadProperty<string>("resStr"));
            }
            catch (JavaScriptException e)
            {
                Colors.WriteLine(e.Message.Replace("Script threw an exception. ", "").DarkRed());
            }
        }
    }
}