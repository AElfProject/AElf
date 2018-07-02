using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetGenesisContractAddressCmd : CliCommandDefinition
    {
        public const string Name = "connect_chain";
        
        public GetGenesisContractAddressCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "connect_chain";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject();
            var req = JsonRpcHelpers.CreateRequest(reqParams, "connect_chain", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp["result"]);
            
            return j.ToString();
        } 
    }
}