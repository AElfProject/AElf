using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetMerklePathCmd : CliCommandDefinition
    {
        private const string Name = "get_merkle_path";

        public GetMerklePathCmd() : base(Name)
        {
        }

        public override string GetUsage()
        {
            return "get_merkle_path <txid>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["txid"] = parsedCmd.Args.ElementAt(0)};
            var req = JsonRpcHelpers.CreateRequest(reqParams, Name, 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp);
            return j.ToString();
        }
    }
}