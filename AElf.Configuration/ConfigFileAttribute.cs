using System;

namespace AElf.Configuration
{
    public class ConfigFileAttribute:Attribute
    {
        public string FileName { get; set; }
    }
}