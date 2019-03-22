using CommandLine;

namespace AElf.CLI.Commands
{
    [Verb("send", HelpText = "Send a transaction to a contract.")]
    public class SendTransactionOption : SendOrCallOption
    {
    }

    public class SendTransactionCommand : SendOrCallBase
    {
        public SendTransactionCommand(SendTransactionOption option) : base(option)
        {
        }
    }
}