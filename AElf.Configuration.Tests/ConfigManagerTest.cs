using System;
using System.Threading;
using Xunit;

namespace AElf.Configuration.Tests
{
    public class ConfigManagerTest
    {
        [Fact]
        public void GetConfigInstance()
        {
            var config1 = ConfigManager.GetConfigInstance<ConfigManagerTestConfig>();
            var config2 = ConfigManager.GetConfigInstance<ConfigManagerTestConfig>();

            Assert.Equal(config1, config2);
        }

        [Fact]
        public void TempTest()
        {
            while (true)
            {
                Console.WriteLine(TestConfig.Instance.Host);
                Thread.Sleep(1000);
            }
        }
    }
    
    public class ConfigManagerTestConfig
    {
        public string StingValue { get; set; }

        public int IntValue { get; set; }
    }
    
    [ConfigFile(FileName = "test.json")]
    public class TestConfig : ConfigBase<TestConfig>
    {
        public DatabaseType Type { get; set; }
        
        public string Host { get; set; }
        
        public int Port { get; set; }
    }
}