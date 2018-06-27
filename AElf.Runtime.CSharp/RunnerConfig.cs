using System.Collections.Generic;

namespace AElf.Runtime.CSharp
{
    public class RunnerConfig : IRunnerConfig
    {
        public string SdkDir { get; set; }
        public IEnumerable<string> BlackList { get; set; }
        public IEnumerable<string> WhiteList { get; set; }
    }
}