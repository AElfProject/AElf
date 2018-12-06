using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command.SideChain
{
    public class WithdrawChainCreationRequestCmd :CliCommandDefinition
    {
        private const string CommandName = "withdraw_chain_creation";

        public WithdrawChainCreationRequestCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return CommandName + "<address> <chain_Id>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 1)
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