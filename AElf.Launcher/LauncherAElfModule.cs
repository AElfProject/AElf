using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using AElf.Common.Module;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel.Types.Common;
using Autofac;
using Easy.MessageHub;
using NLog;

namespace AElf.Launcher
{
    public class LauncherAElfModule:IAElfModule
    {
        private static readonly ILogger _logger = LogManager.GetLogger("Launcher");
        private int _stopped;
        private readonly AutoResetEvent _closing = new AutoResetEvent(false);
        private readonly Queue<TerminatedModuleEnum> _modules = new Queue<TerminatedModuleEnum>();
        private TerminatedModuleEnum _prepareTerminatedModule;
        private static System.Timers.Timer _timer;
        
        public void Init(ContainerBuilder builder)
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
            if (_modules.Count != 0)
            {
                _closing.Set();
                PublishMessage();
                _closing.WaitOne();
            }
            else
            {
                _closing.Set();
            }
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            OnModuleTerminated(new TerminatedModule(_prepareTerminatedModule));
        }

        public void Run(ILifetimeScope scope)
        {
            _closing.WaitOne();
        }

        private void OnCancelKeyPress(object sender, EventArgs args)
        {
            if (_modules.Count != 0)
            {
                PublishMessage();
            }
            else
            {
                _closing.Set();
            }
        }

        private void OnModuleTerminated(TerminatedModule moduleTerminated)
        {
            Task.Run(() =>
            {
                _timer.Stop();
                if (_prepareTerminatedModule == moduleTerminated.Module)
                {
                    _modules.Dequeue();
                    _logger.Trace($"{_prepareTerminatedModule.ToString()} stopped.");
                }
                else
                {
                    throw new Exception("Termination error");
                }

                if (_modules.Count == 0)
                {
                    _logger.Trace("node will be closed after 5s...");
                    for (var i = 0; i < 5; i++)
                    {
                        _logger.Trace($"{5 - i}");
                        Thread.Sleep(1000);
                    }

                    _logger.Trace("node is closed.");
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
            _logger.Trace($"begin stop {_prepareTerminatedModule.ToString()}...");
            MessageHub.Instance.Publish(new TerminationSignal(_prepareTerminatedModule));
            
            _timer.Start();
        }
    }
}