using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AElf.Runtime.CSharp
{
    public class RunnerConfig : IRunnerConfig
    {
        public static RunnerConfig FromJObject(JObject jObject)
        {
            var obj = new RunnerConfig();
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