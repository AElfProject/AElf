using System;
using System.IO;
using System.Linq;
using AElf.CLI.Data.Protobuf;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using AElf.Common.ByteArrayHelpers;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using Method = AElf.CLI.Data.Protobuf.Method;

namespace AElf.CLI.Command
{
    public class LoadContractAbiCmd : CliCommandDefinition
    {
        public const string Name = "load_contract_abi";
        
        public LoadContractAbiCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "load_contract_abi <contractAddress>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args.Count > 1)
            {
                return "Invalid number of arguments.";
            }

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject
            {
                ["address"] = parsedCmd.Args.ElementAt(0)
            };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_contract_abi", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            JToken ss = resp["abi"];
            byte[] aa = ByteArrayHelpers.FromHexString(ss.ToString());
            
            MemoryStream ms = new MemoryStream(aa);
            Module m = Serializer.Deserialize<Module>(ms);

            return JsonConvert.SerializeObject(m);
        }
    }
}