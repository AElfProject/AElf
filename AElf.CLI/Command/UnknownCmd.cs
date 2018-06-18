using AElf.CLI.Parsing;

namespace AElf.CLI.Command
{
    class UnknownCmd : CliCommandDefinition
    {
        private const string CommandName = "unknown";
        public UnknownCmd() : base(CommandName)
        {
        }

        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return "command not found.";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return parsedCmd.Command.ToString() + " command not found";
        }
    }
}