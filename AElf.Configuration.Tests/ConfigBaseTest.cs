using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Xunit;

namespace AElf.Configuration.Tests
{
    public class ConfigBaseTest:IDisposable
    {
        private readonly List<string> _fileNames = new List<string>();

        [Fact]
        public void FromFileTest()
        {
            var fileName = "testfromfile.json";
            CheckAndCreateFile(fileName);
            
            var sringValue = TestFromFileConfig.Instance.StingValue;
            var intValue = TestFromFileConfig.Instance.IntValue;

            Assert.True(sringValue == "sting" && intValue == 9);
        }
        
        [Fact]
        public void NoFileTest()
        {
            var sringValue = TestNoFileConfig.Instance.StingValue;
            var intValue = TestNoFileConfig.Instance.IntValue;

            Assert.True(sringValue == null && intValue == 0);
        }
        
        [Fact]
        public void DefaultValueTest()
        {
            var fileName = "testdefaultvalue.json";
            CheckAndCreateFile(fileName);
            
            var sringValue = TestDefaultValueConfig.Instance.StingValue;

            Assert.True(sringValue == "sting");
        }
        
        [Fact]
        public void SetValueTest()
        {
            var fileName = "testsetvalue.json";
            CheckAndCreateFile(fileName);

            TestSetValueConfig.Instance.StingValue = "SetValue";
            var sringValue = TestSetValueConfig.Instance.StingValue;

            Assert.True(sringValue == "SetValue");
        }

        private void CheckAndCreateFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            if (!Directory.Exists(ConfigManager.ConfigFilePaths[0]))
            {
                Directory.CreateDirectory(ConfigManager.ConfigFilePaths[0]);
            }
            if (!File.Exists(filePath))
            {
                File.AppendAllText(filePath, "{\"stringvalue\":\"sting\",\"IntValue\":9}");
                _fileNames.Add(fileName);
            }
        }

        private void DeleteFiles(List<string> fileNames)
        {
            foreach (var fileName in fileNames)
            {
                DeleteFile(fileName);
            }
        }

        private void DeleteFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void Dispose()
        {
            DeleteFiles(_fileNames);
        }
    }
    
    [ConfigFile(FileName = "testfromfile.json")]
    public class TestFromFileConfig : ConfigBase<TestFromFileConfig>
    {
        [JsonProperty("stringvalue")] 
        public string StingValue { get; set; }

        public int IntValue { get; set; }
    }
    
    [ConfigFile(FileName = "nothing.json")]
    public class TestNoFileConfig : ConfigBase<TestNoFileConfig>
    {
        [JsonProperty("stringvalue")] 
        public string StingValue { get; set; }

        public int IntValue { get; set; }
    }
    
    [ConfigFile(FileName = "testdefaultvalue.json")]
    public class TestDefaultValueConfig : ConfigBase<TestDefaultValueConfig>
    {
        [JsonProperty("stringvalue")] 
        public string StingValue { get; set; }

        public int IntValue { get; set; }

        public TestDefaultValueConfig()
        {
            StingValue = "DefaultValue";
        }
    }
    
    [ConfigFile(FileName = "testsetvalue.json")]
    public class TestSetValueConfig : ConfigBase<TestSetValueConfig>
    {
        [JsonProperty("stringvalue")] 
        public string StingValue { get; set; }

        public int IntValue { get; set; }
    }
}