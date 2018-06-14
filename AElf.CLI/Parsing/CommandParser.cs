using System.Collections.Generic;

namespace AElf.CLI.Parsing
{
    public class CommandParser
    {
        public CmdParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string str = input.TrimEnd();
            
            string[] tokens = str.Split();

            if (tokens.Length < 1)
                return null;
            
            CmdParseResult res = new CmdParseResult();
            res.Command = tokens[0];

            res.Args = new List<string>();
            for (int i = 1; i < tokens.Length; i++)
            {
                res.Args.Add(tokens[i]);
            }

            return res;
        }
    }
}