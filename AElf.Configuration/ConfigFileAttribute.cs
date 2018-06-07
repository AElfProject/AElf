using System;
using System.Data;

namespace AElf.Configuration
{
    public class ConfigFileAttribute:Attribute
    {
        public string FileName { get; set; }
    }
}