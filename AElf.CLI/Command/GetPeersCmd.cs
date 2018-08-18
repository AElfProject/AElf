using System;
using System.Linq;
using System.Text;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetPeersCmd : CliCommandDefinition
    {
        private const string CommandName = "get_peers";
        
        public GetPeersCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return "usage: get_peers <number of peers>";
        }

        public override string GetUsage(string subCommand)
        {
            throw new System.NotImplementedException();
        }
        
        public override string GetUrl()
        {
            return "/net";
        }

        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            JObject reqParams;
            
            if (parsedCmd.Args == null || parsedCmd.Args.Count <= 0)
                 reqParams = new JObject { ["numPeers"] = null };
            else
                reqParams = new JObject { ["numPeers"] = parsedCmd.Args.ElementAt(0) };

            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_peers", 1);
            
            return req;
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override string GetPrintString(JObject resp)
        {
            StringBuilder strBuilder = new StringBuilder();
            
            try
            {
                int authCount = resp["auth"].ToObject<int>();
                strBuilder.AppendLine($"Nodes connected ({authCount} authentifying) :");
                
                JArray peersList = JArray.Parse(resp["nodeData"].ToString());

                foreach (var p in peersList.Children())
                {
                    strBuilder.AppendLine(p["IpAddress"] + ":" + p["Port"]);
                }
            }
            catch (Exception e)
            {
                ;
            }
            
            return strBuilder.ToString();
        }
    }
}