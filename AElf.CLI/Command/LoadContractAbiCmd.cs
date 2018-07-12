using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace AElf.CLI.Command
{
    public class LoadContractAbiCmd : CliCommandDefinition
    {
        public const string CommandName = "load_contract_abi";
        
        public LoadContractAbiCmd() : base(CommandName)
        {
            
        }

        public override string GetUsage()
        {
            return "load_contract_abi <contractAddress>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return parsedCmd.Args.Count > 1 ? "Invalid number of arguments." : null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["address"] = parsedCmd.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_contract_abi", 1);

            return req;
        }

        /*public override string GetPrintString(JObject resp)
        {
            JToken ss = resp["abi"];
            byte[] aa = ByteArrayHelpers.FromHexString(ss.ToString());
            
            MemoryStream ms = new MemoryStream(aa);
            Module m = Serializer.Deserialize<Module>(ms);

            return JsonConvert.SerializeObject(m);          
        }*/
    }
}