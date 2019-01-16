using System.Collections.Generic;

namespace AElf.Runtime.CSharp
{
    public class RunnerOptions
    {
        public string SdkDir { get; set; }
        public List<string> BlackList { get; set; }
        public List<string> WhiteList { get; set; }
    }
}