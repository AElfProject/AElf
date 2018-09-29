using AElf.CLI.Parsing;

namespace AElf.CLI.Command
{
    public class VerifyTransactionCmd : CliCommandDefinition
    {
        private const string Name = "verify_tx";
        public VerifyTransactionCmd() : base(Name)
        {
        }

        public override string GetUsage()
        {
            return "verify_tx <tx> <merkle_path> <height>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
    }
}