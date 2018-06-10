using System;
using System.Text;
using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetIncrementCmd : CliCommandDefinition
    {
        private const string CommandName = "get-increment";
        
        public GetIncrementCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return "usage get-increment <address>";
        }

        public override string Validate(CmdParseResult parsedCommand)
        {
            if (parsedCommand == null)
                return "no command";

            if (parsedCommand.Args == null || parsedCommand.Args.Count <= 0)
                return "not enough arguments";

            return null;
        }
        
        public override string GetPrintString(string resp)
        {
            JObject respJson = JObject.Parse(resp);
            string increment = respJson["increment"].ToString();
            return increment;
        }
    }
}