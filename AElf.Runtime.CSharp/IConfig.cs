using System.Collections.Generic;

namespace AElf.Runtime.CSharp
{
    public interface IConfig
    {
        string SdkDir { get; set; }
        IEnumerable<string> BlackList { get; set; }
        IEnumerable<string> WhiteList { get; set; }
    }
}