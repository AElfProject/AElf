using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class CallReadOnlyCmd : CliCommandDefinition
    {
        private const string CommandName = "call";
        
        public CallReadOnlyCmd() : base(CommandName)
        {
        }

        public override bool IsLocal { get; } = true;

        public override string GetUsage()
        {
            return "usage: call <address>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd == null)
                return "no command\n" + GetUsage();

            if (parsedCmd.Args == null || parsedCmd.Args.Count <= 0)
                return "not enough arguments\n" + GetUsage();

            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject { ["address"] = parsedCmd.Args.ElementAt(0) };
            var req = JsonRpcHelpers.CreateRequest(reqParams, "call", 1);

            return req;
        }
        
        public override string GetPrintString(JObject resp)
        {
            return resp.ToString();
        }
    }
}