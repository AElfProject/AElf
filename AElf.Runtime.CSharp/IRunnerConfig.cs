using System.Collections.Generic;

namespace AElf.Runtime.CSharp
{
    // todo zx lr
    public interface IRunnerConfig
    {
        string SdkDir { get; set; }
        IEnumerable<string> BlackList { get; set; }
        IEnumerable<string> WhiteList { get; set; }
    }
}