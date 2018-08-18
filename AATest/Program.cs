using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Configuration;

namespace AATest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                FileChangeTest();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                Console.ReadKey();
            }
        }
        
        public static void FileChangeTest()
        {
            var fileName = "test.json";
            CheckAndCreateFile(fileName);
            
            Console.WriteLine(TestConfig.Instance.StringValue);

            ChangeFile(fileName);
            Thread.Sleep(6000);
            
            Console.WriteLine(TestConfig.Instance.StringValue);
                        
            DeleteFile(fileName);
        }
        
        private static void CheckAndCreateFile(string fileName)
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

        private static void ChangeFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            File.WriteAllText(filePath, "{\"StringValue\":\"str-b\"}");
        }
        
        private static void DeleteFile(string fileName)
        {
            var filePath = Path.Combine(ConfigManager.ConfigFilePaths[0],fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
    
//    [ConfigFile(FileName = "test.json")]
//    public class TestConfig : ConfigBase<TestConfig>
//    {
//        public DatabaseType Type { get; set; }
//        
//        public string Host { get; set; }
//        
//        public int Port { get; set; }
//
//        static TestConfig()
//        {
//            ConfigChanged += Changed;
//        }
//
//        private static void Changed(object sender, EventArgs e)
//        {
//            Console.WriteLine("config changed: "+ TestConfig.Instance.Host);
//        }
//    }
    [ConfigFile(FileName = "test2.json")]
    public class Test2Config : ConfigBase<Test2Config>
    {
        public DatabaseType Type { get; set; }
        
        public string Host { get; set; }
        
        public int Port { get; set; }
        
        static Test2Config()
        {
            ConfigChanged += Changed;
        }

        private static void Changed(object sender, EventArgs e)
        {
            Console.WriteLine("config changed: "+ Test2Config.Instance.Host);
        }
    }
    
    
    [ConfigFile(FileName = "test.json")]
    public class TestConfig : ConfigBase<TestConfig>
    {
        public string StringValue { get; set; }
        
        static TestConfig()
        {
            ConfigChanged += Changed;
        }

        private static void Changed(object sender, EventArgs e)
        {
            Console.WriteLine("config changed: "+ TestConfig.Instance.StringValue);
        }
    }
}