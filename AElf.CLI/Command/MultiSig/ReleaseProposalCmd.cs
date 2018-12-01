using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command.MultiSig
{
    public class ReleaseProposalCmd : CliCommandDefinition
    {
        private const string CommandName = "release_proposal";
        
        public ReleaseProposalCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return CommandName + " <address> <proposal_id>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 2)
            {
                return "Wrong arguments." + GetUsage();
            }
            return null;
        }
        
        public override string GetPrintString(JObject jObj)
        {
            string hash = jObj["hash"] == null ? jObj["error"].ToString() :jObj["hash"].ToString();
            string res = jObj["hash"] == null ? "error" : "txId";
            var jobj = new JObject
            {
                [res] = hash
            };
            return jobj.ToString();
        }
    }
}