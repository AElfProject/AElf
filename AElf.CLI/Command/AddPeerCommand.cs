using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class AddPeerCommand : CliCommandDefinition
    {
        private const string CommandName = "add_peer";

        public AddPeerCommand() : base(CommandName)
        {
        }
        
        public override string GetUsage()
        {
            return "usage: add_peer <IP:port>";
        }
        
        public override string GetUrl()
        {
            return "/net";
        }
        
        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 1)
            {
                return "Wrong arguments";
            }

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            JObject reqParams;
            
            reqParams = new JObject { ["address"] = parsedCmd.Args.ElementAt(0) };

            var req = JsonRpcHelpers.CreateRequest(reqParams, "add_peer", 1);
            
            return req;
        }
    }
}