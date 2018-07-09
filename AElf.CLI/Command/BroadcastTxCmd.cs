using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class SendTransactionCmd : CliCommandDefinition
    {
        private const string CommandName = "broadcast_tx";
        
        public SendTransactionCmd() : base(CommandName)
        {
        }
       
        public override string GetUsage()
        {
            return "usage: broadcast_tx <address>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd == null)
                return "no command\n" + GetUsage();

            if (parsedCmd.Args == null || parsedCmd.Args.Count <= 0)
                return "not enough arguments\n" + GetUsage();

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["rawtx"] = parsedCmd.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "broadcast_tx", 1);

            return req;
        }
        
        public override string GetPrintString(JObject resp)
        {
            string hash = resp["hash"] == null ? resp["error"].ToString() :resp["hash"].ToString();
            string res = resp["hash"] == null ? "error" : "txId";
            var jobj = new JObject
            {
                [res] = hash
            };
            return jobj.ToString();
        }
    }
}