using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command.MultiSig
{
    public class ProposeCmd : CliCommandDefinition
    {
        private const string CommandName = "propose";
        public ProposeCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return CommandName + " <address> <multi-sig_address> <proposal_name> <seconds> <proposed_transaction>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 5)
            {
                return "Wrong arguments. " + GetUsage();
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