using System.Collections.Generic;
using AElf.CLI.Parsing;

namespace AElf.CLI.Command.Account
{
    public class AccountCmd : CliCommandDefinition
    {
        private const string CommandName = "account";
        
        public AccountCmd() : base(CommandName)
        {
        }
        
        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return "usage";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
    }
}