using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Module;
using AElf.Kernel.Types.Common;
using Autofac;
using Easy.MessageHub;
using NLog;

namespace AElf.Launcher
{
    public class LauncherAElfModule:IAElfModule
    {
        private static readonly ILogger Logger = LogManager.GetLogger("Launcher");
        private readonly AutoResetEvent _closing = new AutoResetEvent(false);
        private readonly Queue<TerminatedModuleEnum> _modules = new Queue<TerminatedModuleEnum>();
        private TerminatedModuleEnum _prepareTerminatedModule;
        
        public void Init(ContainerBuilder builder)
        {
            MessageHub.Instance.Subscribe<TerminatedModule>(OnModuleTerminated);
            
            _modules.Enqueue(TerminatedModuleEnum.Rpc);
            _modules.Enqueue(TerminatedModuleEnum.TxPool);
            _modules.Enqueue(TerminatedModuleEnum.Mining);
            _modules.Enqueue(TerminatedModuleEnum.BlockSynchronizer);
            _modules.Enqueue(TerminatedModuleEnum.BlockExecutor);
            _modules.Enqueue(TerminatedModuleEnum.BlockRollback);
        }

        public void Run(ILifetimeScope scope)
        {
            Console.CancelKeyPress += OnExit;
            _closing.WaitOne();
        }

        private void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            if (_modules.Count != 0)
            {
                PublishMessage();
            }
        }

        private void OnModuleTerminated(TerminatedModule moduleTerminated)
        {
            Task.Run(() =>
            {
                if (_prepareTerminatedModule == moduleTerminated.Module)
                {
                    _modules.Dequeue();
                    Logger.Trace($"{_prepareTerminatedModule.ToString()} stopped.");
                }
                else
                {
                    throw new Exception("Termination error");
                }

                if (_modules.Count == 0)
                {
                    Logger.Trace("node will be shut down after 5s...");
                    for (var i = 0; i < 5; i++)
                    {
                        Logger.Trace($"{5 - i}");
                        Thread.Sleep(1000);
                    }

                    Logger.Trace("node is shut down.");
                    _closing.Set();
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
            Logger.Trace($"begin stop {_prepareTerminatedModule.ToString()}...");
            MessageHub.Instance.Publish(new TerminationSignal(_prepareTerminatedModule));
        }
    }
}