using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using Autofac;

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
                Type = DatabaseType.Redis,
                Host = "127.0.0.1",
                Port = 6379
            };
            builder.RegisterModule(new WorldStateDictatorModule());
            builder.RegisterModule(new DatabaseModule(dataConfig));
            builder.RegisterModule(new LoggerModule());
            builder.RegisterType<Benchmarks>().WithParameter("chainId", chainId).WithParameter("maxTxNum", 3000);
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
                var resDict = new Dictionary<string, double>();
                int groupCount = 8;
                for (int i = 1; i <= groupCount; i++)
                {
                    var res = await benchmarkTps.MultipleGroupBenchmark(2400, i);
                    resDict.Add(res.Key, res.Value);
                }

                resDict.ForEach((info, time) => Console.WriteLine(info + ": " + time));
            }
        }
        
        private static bool CheckDBConnect(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            return db.IsConnected();
        }

        private static void PrintHelperAndExit()
        {
            Console.WriteLine("Please input valid arguments, example: [ -scExec -${path-to-contract-dll} -evenGroup 4000 1 4");
        }
    }
}