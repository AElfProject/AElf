using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Enums;
using AElf.Execution;
using AElf.Configuration;
using AElf.Database;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Akka.Actor;
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
                .WithParsed(o => { opts = o; })
                .WithNotParsed(errs => {});

            if (opts == null)
            {
                return;
            }

            if (opts.BenchmarkMethod == BenchmarkMethod.EvenGroup)
            {
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

                if (!Directory.Exists(Path.GetFullPath(opts.SdkDir)))
                {
                    Console.WriteLine("directory " + Path.GetFullPath(opts.SdkDir) + " not exist");
                    return;
                }

                if (!File.Exists(Path.GetFullPath(Path.Combine(opts.DllDir, opts.ContractDll))))
                {
                    Console.WriteLine(
                        Path.GetFullPath(Path.Combine(opts.DllDir, opts.ContractDll) +
                                         " not exist"));
                    return;
                }

                if (!File.Exists(Path.GetFullPath(Path.Combine(opts.DllDir, opts.ZeroContractDll))))
                {
                    Console.WriteLine(
                        Path.GetFullPath(Path.Combine(opts.DllDir, opts.ZeroContractDll) +
                                         " not exist"));
                    return;
                }

                if (opts.GroupRange.Count() != 2 || opts.GroupRange.ElementAt(0) > opts.GroupRange.ElementAt(1))
                {
                    Console.WriteLine(
                        "please input 2 number to indicate the lower and upper bounds, where lower bound is lower than upper bound");
                    return;
                }

                try
                {
                    DatabaseConfig.Instance.Type = DatabaseTypeHelper.GetType(opts.Database);
                }
                catch (ArgumentException)
                {
                    Console.WriteLine(
                        $"Database {opts.Database} not supported, use one of the following databases: [keyvalue, redis, ssdb]");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(opts.DbHost))
                {
                    DatabaseConfig.Instance.Host = opts.DbHost;
                }

                if (opts.DbPort.HasValue)
                {
                    DatabaseConfig.Instance.Port = opts.DbPort.Value;
                }

                if (opts.ConcurrencyLevel.HasValue)
                {
                    ActorConfig.Instance.ConcurrencyLevel = opts.ConcurrencyLevel.Value;
                }

                //enable parallel feature for benchmark
                ParallelConfig.Instance.IsParallelEnable = true;

                var builder = new ContainerBuilder();
                //builder.RegisterModule(new MainModule());
                builder.RegisterModule(new StateDictatorModule());
                builder.RegisterModule(new DatabaseAutofacModule());
                builder.RegisterModule(new LoggerModule());
                builder.RegisterModule(new StorageModule());
                builder.RegisterModule(new ServicesModule());
                builder.RegisterModule(new KernelAutofacModule());
                builder.RegisterModule(new MetadataModule());
                builder.RegisterType<Benchmarks>().WithParameter("options", opts);
                var runner = new SmartContractRunner(opts.SdkDir);
                SmartContractRunnerFactory smartContractRunnerFactory = new SmartContractRunnerFactory();
                smartContractRunnerFactory.AddRunner(0, runner);
                smartContractRunnerFactory.AddRunner(1, runner);
                builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();

                if (ParallelConfig.Instance.IsParallelEnable)
                {
                    builder.RegisterType<Grouper>().As<IGrouper>();
                    builder.RegisterType<ServicePack>().As<ServicePack>().PropertiesAutowired();
                    builder.RegisterType<ActorEnvironment>().As<IActorEnvironment>().SingleInstance();
                    builder.RegisterType<ParallelTransactionExecutingService>().As<IExecutingService>();
                }
                else
                {
                    builder.RegisterType<SimpleExecutingService>().As<IExecutingService>();
                }
                
                var container = builder.Build();

                if (container == null)
                {
                    Console.WriteLine("IoC setup failed");
                    return;
                }

                if (!CheckDbConnect(container))
                {
                    Console.WriteLine("Database connection failed");
                    return;
                }

                using (var scope = container.BeginLifetimeScope())
                {
                    if (ParallelConfig.Instance.IsParallelEnable)
                    {
                        var actorEnv = scope.Resolve<IActorEnvironment>();
                        try
                        {
                            actorEnv.InitActorSystem();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                           
                    }

                    var benchmarkTps = scope.Resolve<Benchmarks>();
                    await benchmarkTps.InitContract();
                    Thread.Sleep(200); //sleep 200 ms to let async console print in order 
                    await benchmarkTps.BenchmarkEvenGroup();
                }
            }
            else if (opts.BenchmarkMethod == BenchmarkMethod.GenerateAccounts)
            {
                var dataGenerator = new TransactionDataGenerator(opts);
                dataGenerator.PersistAddrsToFile(opts.AccountFileDir);
            }

            Console.WriteLine("\n\nPress any key to continue ");
            Console.ReadKey();
        }

        private static bool CheckDbConnect(IComponentContext container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            try
            {
                return db.IsConnected();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
        }
    }
}