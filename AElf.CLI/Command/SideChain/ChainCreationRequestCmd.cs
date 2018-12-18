using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command.SideChain
{
    public class ChainCreationRequestCmd: CliCommandDefinition
    {
        private const string CommandName = "request_chain_creation";

        public ChainCreationRequestCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return CommandName + " <address> <locked_amount> <indexing_price> <contract_name>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 4)
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