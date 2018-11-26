using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class DeployContractCommand : CliCommandDefinition
    {
        private const string Name = "deploy_contract";
        
        public DeployContractCommand() : base(Name)
        {
        }

        public override string GetUsage()
        {
            return "deploy_contract <category> <filename>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 2)
            {
                return "Wrong arguments";
            }

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            /*var reqParams = new JObject { ["address"] = parsedCmd.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_increment", 1);*/

            return null;
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