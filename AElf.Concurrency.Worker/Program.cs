﻿using System;
using AElf.Database;
using AElf.Database.Config;
using AElf.Kernel.Concurrency;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner;
using AElf.Kernel.Modules.AutofacModule;
using AElf.Kernel.Node.Config;
using AElf.Kernel.TxMemPool;
using AElf.Launcher;
using AElf.Network.Config;
using AElf.Runtime.CSharp;
using Autofac;

namespace AElf.Concurrency.Worker
{
    class Program
    {

        static void Main(string[] args)
        {
            // Parse options
            ConfigParser confParser = new ConfigParser();
            bool parsed;
            try
            {
                parsed = confParser.Parse(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            if (!parsed)
                return;
            
            var netConf = confParser.NetConfig;
            var isMiner = confParser.IsMiner;
            
            // Setup ioc 
            IContainer container = SetupIocContainer(isMiner, netConf);

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
                var service = scope.Resolve<IConcurrencyExecutingService>();
                service.InitWorkActorSystem();
                Console.ReadLine();
            }
            
        }

        private static IContainer SetupIocContainer(bool isMiner, IAElfNetworkConfig netConf)
        {
            var builder = new ContainerBuilder();
            
            builder.RegisterModule(new MainModule()); // todo : eventually we won't need this
            
            // Module registrations
            builder.RegisterModule(new TransactionManagerModule());
            builder.RegisterModule(new LoggerModule());
            builder.RegisterModule(new DatabaseModule());
            builder.RegisterModule(new NetworkModule(netConf, isMiner));
            builder.RegisterModule(new RpcServerModule());
            builder.RegisterModule(new MinerModule(null));
            builder.RegisterModule(new MetadataModule());
            builder.RegisterModule(new WorldStateDictatorModule());
            
#if DEBUG
            var runner = new SmartContractRunner("../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
#else
            var runner = new SmartContractRunner("../AElf.SDK.CSharp/bin/Release/netstandard2.0/");
            #endif
            SmartContractRunnerFactory smartContractRunnerFactory = new SmartContractRunnerFactory();
            smartContractRunnerFactory.AddRunner(0, runner);
            smartContractRunnerFactory.AddRunner(1, runner);
            builder.RegisterInstance(smartContractRunnerFactory).As<ISmartContractRunnerFactory>().SingleInstance();
            
            IContainer container = null;
            
            try
            {
                container = builder.Build();
            }
            catch (Exception e)
            {
                return null;
            }

            return container;
        }

        private static bool CheckDBConnect(IContainer container)
        {
            var db = container.Resolve<IKeyValueDatabase>();
            return db.IsConnected();
        }
        
    }
}