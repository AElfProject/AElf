using System.IO;

namespace AElf.Runtime.CSharp;

public class RunnerOptions
{
    public RunnerOptions()
    {
        SdkDir = Path.GetDirectoryName(typeof(RunnerOptions).Assembly.Location);
    }

    public string SdkDir { get; set; }
}