using System;
using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("get-blk-info", HelpText = "Get the block info for a block height.")]
    public class GetBlockInfoOption : BaseOption
    {
        [Value(0, HelpText = "The height of the block to query.", Required = true)]
        public ulong Height { get; set; }

        [Value(1, HelpText = "Whether to include transactions.")]
        public bool IncludeTxs { get; set; } = false;
    }

    public class GetBlockInfoCommand : Command
    {
        private GetBlockInfoOption _option;

        public GetBlockInfoCommand(GetBlockInfoOption option) : base(option)
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


            if (_option.Height == 0)
            {
                Colors.WriteLine("Invalid block heigth is provided.".DarkRed());
                return;
            }

            try
            {
                var incltx = _option.IncludeTxs ? "true" : "false";
                // Get res
                _engine.RunScript($@"
                    var res = aelf.chain.getBlockInfo({_option.Height}, {incltx});
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