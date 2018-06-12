using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class SendTransactionCmd : CliCommandDefinition
    {
        private const string CommandName = "send-tx";
        
        public SendTransactionCmd() : base(CommandName)
        {
        }

        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return "usage send-tx <address>";
        }

        public override string Validate(CmdParseResult parsedCommand)
        {
            if (parsedCommand == null)
                return "no command";

            if (parsedCommand.Args == null || parsedCommand.Args.Count <= 0)
                return "not enough arguments";

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCommand)
        {
            var reqParams = new JObject { ["address"] = parsedCommand.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_increment", 1);

            return req;
        }
        
        public override string GetPrintString(JObject resp)
        {
            string increment = resp["increment"].ToString();
            return increment;
        }
    }
}