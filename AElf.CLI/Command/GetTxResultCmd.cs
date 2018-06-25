using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetTxResultCmd : CliCommandDefinition
    {
        public const string Name = "get-tx-result";
        
        public GetTxResultCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "get-tx-result <txhash>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 1)
            {
                return "Invalid number of arguments.";
            }

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["txhash"] = parsedCmd.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_tx_result", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            return resp["txresult"].ToString();
        }
    }
}