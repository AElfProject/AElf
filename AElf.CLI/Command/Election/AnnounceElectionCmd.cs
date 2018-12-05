using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command.Election
{
    public class AnnounceElectionCmd : CliCommandDefinition
    {
        private new const string Name = "announce_election";
        
        public AnnounceElectionCmd() : base(Name)
        {
            
        }

        public override string GetUsage()
        {
            return "usage: announce_election";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject();
            var req = JsonRpcHelpers.CreateRequest(reqParams, "announce_election", 1);

            return req;
        }

        public override string GetPrintString(JObject resp)
        {
            var j = JObject.FromObject(resp["result"]);
            
            return j.ToString();
        }
    }
}