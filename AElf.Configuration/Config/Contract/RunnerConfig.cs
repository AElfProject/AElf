using System.Collections.Generic;

namespace AElf.Configuration.Config.Contract
{
    [ConfigFile(FileName = "runner.json")]
    public class RunnerConfig:ConfigBase<RunnerConfig>
    {
        public string SdkDir { get; set; }
        public List<string> BlackList { get; set; }
        public List<string> WhiteList { get; set; }
    }
}