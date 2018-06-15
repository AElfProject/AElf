using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.CLI.Parsing;
using AElf.CLI.RPC;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetCommandsCmd : CliCommandDefinition
    {
        private const string CommandName = "get_commands";
        
        public GetCommandsCmd() : base(CommandName)
        {
        }

        public override string GetUsage()
        {
            return "usage: get_commands";
        }

        public override string GetUsage(string subCommand)
        {
            throw new System.NotImplementedException();
        }

        public override JObject BuildRequest(CmdParseResult parsedCmd)
        {
            JObject reqParams = new JObject {};

            var req = JsonRpcHelpers.CreateRequest(reqParams, "get_commands", 0);
            
            return req;
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            return null;
        }
        
        public override string GetPrintString(JObject resp)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.AppendLine("-- List of commands on the node");
            List<string> commands;
            
            try
            {
                var comms = resp["commands"].ToList();
                
                commands = comms.Select(c => (string) c).ToList();

                foreach (var c in commands)
                {
                    strBuilder.AppendLine(c);
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