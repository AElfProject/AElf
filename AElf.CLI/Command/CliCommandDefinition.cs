using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public abstract class CliCommandDefinition
    {
        public string Name { get; }
        public string Usage { get; }
    
        public CliCommandDefinition(string name, string usage)
        {
            
        }

        public abstract JObject BuildRequestObject(CmdParseResult ParsedCommand);
    }
}