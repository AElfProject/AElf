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
    }
    
    public class ConfigManagerTestConfig
    {
        public string StingValue { get; set; }

        public int IntValue { get; set; }
    }
}