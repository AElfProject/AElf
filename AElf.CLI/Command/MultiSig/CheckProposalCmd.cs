using System.Linq;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command.MultiSig
{
    public class CheckProposalCmd : CliCommandDefinition
    {
        public const string CommandName = "check_proposal";
        public CheckProposalCmd() : base(CommandName)
        {
        }
        
        public override string GetUsage()
        {
            return CommandName + " <proposal_id>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 1)
            {
                return "Wrong arguments. " + GetUsage();
            }
            return null;
        }
        
        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            var reqParams = new JObject
            {
                ["proposal_id"] = parsedCmd.Args.ElementAt(0)
            };
            var req = JsonRpcHelpers.CreateRequest(reqParams, CommandName, 1);

            return req;
        }
        
        public override string GetPrintString(JObject jObj)
        {
            return jObj.ToString();
        }
    }
}