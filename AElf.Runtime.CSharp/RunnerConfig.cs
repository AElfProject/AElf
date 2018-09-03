using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AElf.Runtime.CSharp
{
    // todo zx lr
    public class RunnerConfig0 : IRunnerConfig
    {
        public static RunnerConfig0 FromJObject(JObject jObject)
        {
            var obj = new RunnerConfig0();
            if (jObject.TryGetValue("sdkdir", out var sdkdir))
            {
                obj.SdkDir = sdkdir.ToString();
            }

            try
            {
                obj.WhiteList = ((JArray) jObject["whitelist"]).ToObject<List<string>>();
            }
            catch (NullReferenceException)
            {
            }
            
            try
            {
                obj.BlackList = ((JArray) jObject["blacklist"]).ToObject<List<string>>();
            }
            catch (NullReferenceException)
            {
            }

            return obj;
        }
        
        public string SdkDir { get; set; }
        public IEnumerable<string> BlackList { get; set; }
        public IEnumerable<string> WhiteList { get; set; }
    }
}