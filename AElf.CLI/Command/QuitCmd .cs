using AElf.CLI.Parsing;

namespace AElf.CLI.Command
{
    class QuitCmd : CliCommandDefinition
    {
        private const string CommandName = "quit";
        public QuitCmd() : base(CommandName)
        {
        }

        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return string.Empty;
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
    }
}