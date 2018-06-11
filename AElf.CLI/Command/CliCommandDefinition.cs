using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public abstract class CliCommandDefinition
    {
        public string Name { get; }
        public virtual bool IsLocal { get; } = false;
    
        public CliCommandDefinition(string name)
        {
            Name = name;
        }
        
        public abstract string GetUsage();
        
        public virtual string GetUsage(string subCommand)
        {
            return string.Empty;
        }

        public virtual JObject BuildRequest(CmdParseResult parsedCommand)
        {
            return null;
        }

        public abstract string Validate(CmdParseResult parsedCommand);

        public virtual string GetPrintString(string resp)
        {
            return string.Empty;
        }
    }
}