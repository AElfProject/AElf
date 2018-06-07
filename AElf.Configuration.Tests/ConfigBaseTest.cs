using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace AElf.Configuration.Tests
{
    public class ConfigBaseTest
    {
        [Fact]
        public void ConfigTest()
        {
        }
    }

    [ConfigFile(FileName = "testconfig.conf")]
    public class TestConfig:ConfigBase<TestConfig>
    {
        [JsonProperty("stringvalue")]
        public string StingValue { get; set; }
        
        public int IntValue { get; set; }

        [JsonProperty("detail")]
        public List<ConfigDetail> DetailList { get; set; }

        public TestConfig()
        {
            StingValue = "ssss";
            IntValue = 111;
        }
    }

    public class ConfigDetail
    {
        public int Id { get; set; }

        public string Value { get; set; }

        public string Remark { get; set; }
    }
}