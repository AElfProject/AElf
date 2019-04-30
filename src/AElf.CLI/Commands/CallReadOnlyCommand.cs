using Alba.CsConsoleFormat.Fluent;
using ChakraCore.NET.API;
using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("call", HelpText = "Send a transaction to a contract.")]
    public class CallReadOnlyOption : SendOrCallOption
    {
    }

    public class CallReadOnlyCommand : SendOrCallBase
    {
        public CallReadOnlyCommand(CallReadOnlyOption option) : base(option)
        {
            _isCall = true;
        }
    }
}