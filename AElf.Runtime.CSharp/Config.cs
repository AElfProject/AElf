using System.Collections.Generic;

namespace AElf.Runtime.CSharp
{
    public class Config : IConfig
    {
        public string SdkDir { get; set; }
        public IEnumerable<string> BlackList { get; set; }
        public IEnumerable<string> WhiteList { get; set; }
    }
}