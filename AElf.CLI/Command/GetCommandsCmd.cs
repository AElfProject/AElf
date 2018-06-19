using AElf.CLI.Parsing;
using System;
using System.Text;

namespace AElf.CLI.Command
{
    public class GetCommandsCmd : CliCommandDefinition
    {
        private const string GetPeersName = "getcommands";
        
        public GetCommandsCmd() : base(GetPeersName)
        {
        }

        public override string GetPrintString(string resp)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("peers\t Displays peers in the network.");
            strBuilder.AppendLine("account\t Displays account information.");
            return strBuilder.ToString();
        }

        public override string GetUsage()
        {
            throw new NotImplementedException();
        }

        public override string Validate(CmdParseResult parsedCommand)
        {
            return null;
        }
    }
}