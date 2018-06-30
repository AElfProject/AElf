using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel;
using AElf.Kernel.Concurrency;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using Akka.Util.Internal;
using Autofac;
using CommandLine;

namespace AElf.Benchmark
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            BenchmarkOptions opts = null;
            Parser.Default.ParseArguments<BenchmarkOptions>(args)
                .WithParsed(o =>
                {
                    opts = o;
                })
                .WithNotParsed(errs =>
                {
                    //Success = false;
                    //error
                });

            if (opts == null)
            {
                return;
            }

            if (opts.SdkDir == null)
            {
                opts.SdkDir = Directory.GetCurrentDirectory();
                Console.WriteLine("No Sdk directory in arg, choose current directory: " + opts.SdkDir);
            }
            
            if (opts.DllDir == null)
            {
                opts.DllDir = Directory.GetCurrentDirectory();
                Console.WriteLine("No dll directory in arg, choose current directory: " + opts.DllDir);
            }

            if (!Directory.Exists(System.IO.Path.GetFullPath(opts.SdkDir)))
            {
                Console.WriteLine("directory " + System.IO.Path.GetFullPath(opts.SdkDir) + " not exist");
                return;
            }
            if (!File.Exists(System.IO.Path.GetFullPath(System.IO.Path.Combine(opts.DllDir, opts.ContractDll))))
            {
                Console.WriteLine(System.IO.Path.GetFullPath(System.IO.Path.Combine(opts.DllDir, opts.ContractDll) + " not exist"));
                return;
            }
            
            if (!File.Exists(System.IO.Path.GetFullPath(System.IO.Path.Combine(opts.DllDir, opts.ZeroContractDll))))
            {
                Console.WriteLine(System.IO.Path.GetFullPath(System.IO.Path.Combine(opts.DllDir, opts.ZeroContractDll) + " not exist"));
                return;
            }
            
            if (opts.GroupRange.Count() != 2 || opts.GroupRange.ElementAt(0) > opts.GroupRange.ElementAt(1))
            {
                Console.WriteLine(
                    "please input 2 number to indicate the lower and upper bounds, where lower bound is lower than upper bound");
                return;
            }
            
            var builder = new ContainerBuilder();
            builder.RegisterModule(new MainModule());
            builder.RegisterModule(new MetadataModule());

            DatabaseConfig.Instance.Type = opts.DatabaseConfig.Type;
            DatabaseConfig.Instance.Host = opts.DatabaseConfig.Host;
            DatabaseConfig.Instance.Port = opts.DatabaseConfig.Port;
            builder.RegisterModule(new WorldStateDictatorModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterType(typeof(ConcurrencyExecutingService)).As<IConcurrencyExecutingService>().SingleInstance();
            builder.RegisterType<Benchmarks>().WithParameter("options", opts);
            var runner = new SmartContractRunner(opts.SdkDir);
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
                var concurrencySercice = scope.Resolve<IConcurrencyExecutingService>();
                concurrencySercice.InitActorSystem();
                
                var benchmarkTps = scope.Resolve<Benchmarks>();
                if (opts.SupportedBenchmark == "evenGroup")
                {
                    await benchmarkTps.BenchmarkEvenGroup();
                }

                Console.WriteLine("Press any key to continue ");
                Console.ReadKey();
            }
        }
        
        private static bool CheckDBConnect(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            return db.IsConnected();
        }
    }
}