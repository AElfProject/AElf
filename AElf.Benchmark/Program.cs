using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Concurrency;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node;
using AElf.Kernel.Services;
using AElf.Runtime.CSharp;
using Autofac;
using Autofac.Core;

namespace AElf.Benchmark
{
    public class Program
    {
        
        public static async Task Main()
        {
            Hash chainId = Hash.Generate();
            var builder = new ContainerBuilder();
            builder.RegisterModule(new MainModule());
            builder.RegisterModule(new MetadataModule());

            var dataConfig = new DatabaseConfig
            {
                Type = DatabaseType.KeyValue,
                Host = "192.168.9.9",
                Port = 6379
            };
            DatabaseConfig.Instance.Type = DatabaseType.Ssdb;
            builder.RegisterModule(new WorldStateDictatorModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterType(typeof(ConcurrencyExecutingService)).As<IConcurrencyExecutingService>().SingleInstance();
            builder.RegisterType<Benchmarks>().WithParameter("chainId", chainId).WithParameter("maxTxNum", 30);
            #if DEBUG
            var runner = new SmartContractRunner("../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            #else
            var runner = new SmartContractRunner("../AElf.SDK.CSharp/bin/Release/netstandard2.0/");
            #endif
            SmartContractRunnerFactory smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

            var container = builder.Build();
            
            if (container == null)
            {
                Console.WriteLine("IoC setup failed");
                return;
            }

            if (!CheckDBConnect(container))
            {
                Console.WriteLine("Database connection failed");
                return;
            }
            
            using(var scope = container.BeginLifetimeScope())
            {
                var benchmarkTps = scope.Resolve<Benchmarks>();

                
                var multiGroupRes = await benchmarkTps.MultipleGroupBenchmark(2, 1);
                foreach (var kv in multiGroupRes)
                {
                    Console.WriteLine(kv.Key + ": " + kv.Value);
                }
            }
        }
        
        private static bool CheckDBConnect(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            return db.IsConnected();
        }
    }
}