using System;
using AElf.ChainController;
using AElf.ChainController.Rpc;
using AElf.Common;
using AElf.Common.Extensions;
using AElf.Common.Module;
using AElf.Database;
using AElf.Execution;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Configuration;
using AElf.Configuration.Config.Network;
using AElf.Miner.Miner;
using AElf.Miner;
using AElf.Net.Rpc;
using AElf.Network;
using AElf.Node;
using AElf.Node.AElfChain;
using AElf.RPC;
using AElf.Runtime.CSharp;
using AElf.SideChain.Creation;
using AElf.SmartContract;
using AElf.Wallet.Rpc;
using Autofac;
using IContainer = Autofac.IContainer;

namespace AElf.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(string.Join(" ", args));

            var parsed = new CommandLineParser();
            parsed.Parse(args);

            var handler = new AElfModuleHandler();
            handler.Register(new DatabaseAElfModule());
            handler.Register(new KernelAElfModule());
            handler.Register(new SmartContractAElfModule());
            handler.Register(new ChainAElfModule());
            handler.Register(new ExecutionAElfModule());
            handler.Register(new NodeAElfModule());
            handler.Register(new MinerAElfModule());
            handler.Register(new NetworkAElfModule());
            handler.Register(new RpcAElfModule());
            handler.Register(new ChainContrallerRpcAElfModule());
            handler.Register(new NetRpcAElfModule());
            handler.Register(new WalletRpcAElfModule());
            handler.Register(new RunnerAElfModule());
            handler.Register(new SideChainAElfModule());
            handler.Register(new LauncherAElfModule());
            handler.Build();
        }
    }
}