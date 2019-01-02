using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AElf.ChainController.Rpc;
using AElf.Configuration.Config.Consensus;
using AElf.Execution;
using AElf.Kernel.Consensus;
using AElf.Kernel.Types.Common;
using AElf.Miner;
using AElf.Miner.Rpc;
using AElf.Modularity;
using AElf.Net.Rpc;
using AElf.Network;
using AElf.Node;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using AElf.SideChain.Creation;
using AElf.Wallet.Rpc;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.FullNodeHosting
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(RuntimeSetupAElfModule),
        
        typeof(RpcChainControllerAElfModule),
        typeof(ExecutionAElfModule),
        typeof(MinerAElfModule),
        typeof(NetRpcAElfModule),
        typeof(NodeAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(SideChainAElfModule),
        typeof(RpcWalletAElfModule),
        typeof(MinerRpcAElfModule),
        typeof(NetworkAElfModule),
        typeof(ConsensusKernelAElfModule))]
    public class FullNodeHostingAElfModule : AElfModule
    {
        public static readonly AutoResetEvent Closing = new AutoResetEvent(false);
        private readonly Queue<TerminatedModuleEnum> _modules = new Queue<TerminatedModuleEnum>();
        private TerminatedModuleEnum _prepareTerminatedModule;
        private static System.Timers.Timer _timer;
        public ILogger<FullNodeHostingAElfModule> Logger { get; set; }

        public FullNodeHostingAElfModule()
        {
            Logger = NullLogger<FullNodeHostingAElfModule>.Instance;
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            MessageHub.Instance.Subscribe<TerminatedModule>(OnModuleTerminated);

            _modules.Enqueue(TerminatedModuleEnum.Rpc);
            _modules.Enqueue(TerminatedModuleEnum.TxPool);
            _modules.Enqueue(TerminatedModuleEnum.Mining);
            _modules.Enqueue(TerminatedModuleEnum.BlockSynchronizer);
            _modules.Enqueue(TerminatedModuleEnum.BlockExecutor);
            _modules.Enqueue(TerminatedModuleEnum.BlockRollback);

            _timer = new System.Timers.Timer(ConsensusConfig.Instance.DPoSMiningInterval * 2);
            _timer.AutoReset = false;
            _timer.Elapsed += TimerOnElapsed;

            AssemblyLoadContext.Default.Unloading += DefaultOnUnloading;
            Console.CancelKeyPress += OnCancelKeyPress;
        }


        private void DefaultOnUnloading(AssemblyLoadContext obj)
        {
            Closing.Set();
            if (_modules.Count != 0)
            {
                PublishMessage();
                Closing.WaitOne();
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            OnModuleTerminated(new TerminatedModule(_prepareTerminatedModule));
        }


        private void OnCancelKeyPress(object sender, EventArgs args)
        {
            if (_modules.Count != 0)
            {
                PublishMessage();
            }
            else
            {
                Closing.Set();
            }
        }


        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
        }

        private void OnModuleTerminated(TerminatedModule moduleTerminated)
        {
            Task.Run(() =>
            {
                _timer.Stop();
                if (_prepareTerminatedModule == moduleTerminated.Module)
                {
                    _modules.Dequeue();
                    Logger.LogTrace($"{_prepareTerminatedModule.ToString()} stopped.");
                }
                else
                {
                    throw new Exception("Termination error");
                }

                if (_modules.Count == 0)
                {
                    Logger.LogTrace("node will be closed after 5s...");
                    for (var i = 0; i < 5; i++)
                    {
                        Logger.LogTrace($"{5 - i}");
                        Thread.Sleep(1000);
                    }

                    Logger.LogTrace("node is closed.");
                    Closing.Set();
                }
                else
                {
                    PublishMessage();
                }
            });
        }

        private void PublishMessage()
        {
            _prepareTerminatedModule = _modules.Peek();
            Logger.LogTrace($"begin stop {_prepareTerminatedModule.ToString()}...");
            MessageHub.Instance.Publish(new TerminationSignal(_prepareTerminatedModule));

            _timer.Start();
        }
    }
}