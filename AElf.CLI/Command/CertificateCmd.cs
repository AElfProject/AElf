using AElf.CLI.Parsing;

namespace AElf.CLI.Command
{
    public class CertificateCmd : CliCommandDefinition
    {
        private const string CommandName = "cert";

        public CertificateCmd() : base(CommandName)
        {
        }
        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return CommandName + "<chain_id> <ip_addr>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
    }
}