using System.Collections.Generic;
using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public abstract class CliCommandDefinition
    {
        public const string InvalidParamsError = "Invalid parameters - See usage";
        
        public string Name { get; }
        public virtual bool IsLocal { get; } = false;

        protected CliCommandDefinition(string name)
        {
            Name = name;
        }
        
        public abstract string GetUsage();
        
        public virtual string GetUsage(string subCommand)
        {
            return string.Empty;
        }

        public virtual JObject BuildRequest(CmdParseResult parsedCmd)
        {
            return null;
        }

        public abstract string Validate(CmdParseResult parsedCmd);

        public virtual string GetPrintString(JObject resp)
        {
            return string.Empty;
        }
    }
}