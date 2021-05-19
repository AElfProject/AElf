using System;
using System.Collections.Generic;

namespace AElf.Cli.Core
{
    public class AElfCliOptions
    {
        public Dictionary<string, Type> Commands { get; }

        public bool CacheTemplates { get; set; } = true;

        public string ToolName { get; set; } = "CLI";

        public AElfCliOptions()
        {
            Commands = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }
    }
}