using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetBlockHeightCmd : CliCommandDefinition
    {
        public const string Name = "get_block_height";
        
        public GetBlockHeightCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "get_block_height";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject();
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_block_height", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp["result"]);
            
            return j.ToString();
        }
    }
}