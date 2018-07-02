using AElf.CLI.Parsing;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.Command
{
    public class GetDeserializedResultCmd : CliCommandDefinition
    {
        public const string Name = "get_deserialized_result";
        
        public GetDeserializedResultCmd() : base(Name)
        {
        }

        public override string GetUsage()
        {
            return "get_deserialized_result <type> <serializeddata>";
        }

        public override string Validate(CmdParseResult parsedCmd)
        {
            if (parsedCmd.Args == null || parsedCmd.Args.Count != 2)
            {
                return "Invalid number of arguments.";
            }

            return null;
        }
        
        
        public override string GetPrintString(JObject resp)
        {
            var jobj = JObject.FromObject(resp["result"]);
            return jobj.ToString();
        }
    }
}