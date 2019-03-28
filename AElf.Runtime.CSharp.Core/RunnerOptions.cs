using System.Collections.Generic;
using System.IO;

namespace AElf.Runtime.CSharp
{
    public class RunnerOptions
    {
        public RunnerOptions()
        {
            SdkDir = Path.GetDirectoryName(typeof(RunnerOptions).Assembly.Location);
        }

        public string SdkDir { get; set; }
        public List<string> BlackList { get; set; }
        public List<string> WhiteList { get; set; }
    }
}