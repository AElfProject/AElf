using System;
using System.IO;
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

        //[Fact]
        public void FileChangeTest()
        {
            var fileName = "test.json";
            CheckAndCreateFile(fileName);

            Assert.Equal(TestConfig.Instance.StringValue, "str-a");

            ChangeFile(fileName);
            Thread.Sleep(6000);
            
            Assert.Equal(TestConfig.Instance.StringValue, "str-b");
            
            DeleteFile(fileName);
        }
        
        private void CheckAndCreateFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            if (!Directory.Exists(ConfigManager.ConfigFilePaths[0]))
            {
                Directory.CreateDirectory(ConfigManager.ConfigFilePaths[0]);
            }
            if (File.Exists(filePath))
            {
                DeleteFile(fileName);
            }
            File.AppendAllText(filePath, "{\"StringValue\":\"str-a\"}");
        }

        private void ChangeFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            File.WriteAllText(filePath, "{\"StringValue\":\"str-b\"}");
        }
        
        private void DeleteFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
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
        public string StringValue { get; set; }
    }
}