using System;
using System.Collections.Generic;

namespace AElf.CLI.Parsing
{
    public class CmdParseResult
    {
        public string Command { get; set; }
        public List<string> Args { get; set; }

        public override string ToString()
        {
            return "cmd: " + Command;
        }
    }
}