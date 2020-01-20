using System.Collections.Generic;
using System.IO;

namespace AElf.Runtime.CSharp
{
    public class RunnerOptions
    {
        public string SdkDir { get; set; }

        public RunnerOptions()
        {
            SdkDir = Path.GetDirectoryName(typeof(RunnerOptions).Assembly.Location);
        }
    }
}