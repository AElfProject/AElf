using System;
using System.Threading;
using AElf.Configuration;

namespace AATest
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var h1 = TestConfig.Instance.Host;
                var h2 = Test2Config.Instance.Host;
                
                
//                while (true)
//                {
//                    Console.WriteLine(DateTime.Now);
//                    Thread.Sleep(1000);
//                }
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
    }
    
    [ConfigFile(FileName = "test.json")]
    public class TestConfig : ConfigBase<TestConfig>
    {
        public DatabaseType Type { get; set; }
        
        public string Host { get; set; }
        
        public int Port { get; set; }

        static TestConfig()
        {
            ConfigChanged += Changed;
        }

        private static void Changed(object sender, EventArgs e)
        {
            Console.WriteLine("config changed: "+ TestConfig.Instance.Host);
        }
    }
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
}