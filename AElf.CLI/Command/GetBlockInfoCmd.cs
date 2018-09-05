using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetBlockInfoCmd : CliCommandDefinition
    {
        public const string Name = "get_block_info";
        
        public GetBlockInfoCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "get_block_info <height> <include_txs>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["block_height"] = parsedCmd.Args.ElementAt(0)};
            if (parsedCmd.Args.Count > 1)
            {
                var arg1 = parsedCmd.Args.ElementAt(1);
                reqParams["include_txs"] = arg1 == "true" || arg1 == "1";
            }
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_block_info", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp["result"]);
            
            return j.ToString();
        }
    }
}