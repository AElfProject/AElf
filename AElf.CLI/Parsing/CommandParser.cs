using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.CLI.Parsing
{
    public class CommandParser
    {
        public CmdParseResult Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string str = input.TrimEnd().TrimStart();

            string[] tokens = str.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length < 1)
                return null;

            return new CmdParseResult
            {
                Command = tokens[0],
                Args = tokens.Skip(1).ToList()
            };
        }
    }
}