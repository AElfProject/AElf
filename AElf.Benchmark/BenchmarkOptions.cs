using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using AElf.Database;
using AElf.Database.Config;
using CommandLine;
using CommandLine.Text;

namespace AElf.Benchmark
{
    public class BenchmarkOptions
    {
        [Option(HelpText = "The directory where store the sdk dll file (usually named AElf.Sdk.CSharp.dll).")]
        public string SdkDir { get; set; }
        
        [Option(HelpText = "The directory where store all the related dll file.")]
        public string DllDir { get; set; }
        
        [Option(Default = "AElf.Benchmark.TestContract.dll", HelpText = "The dll file name that contains the token transfer contract.")]
        public string ContractDll { get; set; }
        
        [Option(Default = "AElf.Contracts.Genesis.dll", HelpText = "The dll file name that contains the contract zero.")]
        public string ZeroContractDll { get; set; }
        
        [Option('f', "SupportedBenchmark", Default = "evenGroup", HelpText = "The benchmark you want to run. Choose one of the following options [evenGroup] (more benchmark method is under development)")]
        public string SupportedBenchmark { get; set; }
        
        [Option('n', "TxNumber", Default = 2400, HelpText = "Transaction number in the benchmark, default is 2400")]
        public int TxNumber { get; set; }
        
        [Option(Default = new []{1, 8}, HelpText = "lower bound and upper bound of the testing group in evenGroup benchmarking, default is [1,8]")]
        public IEnumerable<int> GroupRange { get; set; }
        
        [Option(Default = 10, HelpText = "how many time to repeat the benchmark to get more stable result, default is 10")]
        public int RepeatTime { get; set; }
        
        [Option(Default = "in-memory", HelpText = "which database to choose [in-memory, redis, ssdb], default is in-memory")]
        public string Database { get; set; }

        [Option(Default = "127.0.0.1", HelpText = "host of the database, default is 127.0.0.1")]
        public string DbHost { get; set; }
        
        [Option(HelpText = "port of the database, default is 8888")]
        public int DbPort { get; set; }
        
        public DatabaseConfig DatabaseConfig
        {
            get
            {
                if (!dbTypes.TryGetValue(Database, out var dbType))
                {
                    Console.WriteLine("non-supported database " + Database + ", choose in-memory instead");
                    dbType = DatabaseType.KeyValue;
                }

                if (!IPAddress.TryParse(DbHost, out var Ip))
                {
                    Console.WriteLine("Not valid ip address, choose 127.0.0.1 as host of database");
                }

                return new DatabaseConfig()
                {
                    Type = dbType,
                    Host = DbHost,
                    Port = DbPort
                };
            }
        }
        
        public static Dictionary<string, DatabaseType> dbTypes = new Dictionary<string, DatabaseType>()
        {
            ["in-memory"] = DatabaseType.KeyValue,
            ["redis"] = DatabaseType.Redis,
            ["ssdb"] = DatabaseType.Ssdb
        };
    }
}