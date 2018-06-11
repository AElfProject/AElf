using System;
using System.Linq;
using System.Text;
using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetPeersCmd : CliCommandDefinition
    {
        private const string GetPeersName = "peers";
        
        public GetPeersCmd() : base(GetPeersName)
        {
        }

        public override string GetUsage()
        {
            throw new System.NotImplementedException();
        }

        public override string GetUsage(string subCommand)
        {
            throw new System.NotImplementedException();
        }

        public override JObject BuildRequestParams(CmdParseResult parsedCommand)
        {
            JObject reqParams;
            
            if (parsedCommand.Args == null || parsedCommand.Args.Count <= 0)
            {
                 reqParams = new JObject
                {
                    ["numPeers"] = null
                };
            }
            else
            {
                reqParams = new JObject
                {
                    ["numPeers"] = parsedCommand.Args.ElementAt(0)
                };
            }
            
            return reqParams;
        }

        public override string Validate(CmdParseResult parsedCommand)
        {
            return null;
        }
        
        public override string GetPrintString(string resp)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("-- List of connected peers on the node");
            
            try
            {
                JObject respJson = JObject.Parse(resp);
            
                var peersList = respJson["result"]["data"];

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