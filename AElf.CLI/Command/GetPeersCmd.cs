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

        public override JObject BuildRequestObject(CmdParseResult parsedCommand)
        {
            // If parsedCommand is not valid return null and let other layers/components print the usage 
            // contained in this instance.

            return null;
        }

        public override string Validate(CmdParseResult parsedCommand)
        {
            throw new System.NotImplementedException();
        }
    }
}