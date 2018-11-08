using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Common.Module;
using AElf.Kernel.Types.Common;
using Autofac;
using Easy.MessageHub;

namespace AElf.Launcher
{
    public class LauncherAElfModule:IAElfModule
    {
        private int _stopped;
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
        }

        public void Run(ILifetimeScope scope)
        {
            Console.CancelKeyPress += OnExit;
            _closing.WaitOne();
        }
        
        protected void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            if (_modules.Count != 0)
            {
                PublishMessage();
            }
        }

        private void OnModuleTerminated(TerminatedModule moduleTerminated)
        {
            if (_modules.Count == 0)
            {
                _closing.Set();
            }
            else if(_prepareTerminatedModule == moduleTerminated.Module)
            {
                PublishMessage();
            }
            else
            {
                throw new Exception("Termination error");
            }
        }

        private void PublishMessage()
        {
            _prepareTerminatedModule = _modules.Dequeue();
            MessageHub.Instance.Publish(new TerminationSignal(_prepareTerminatedModule));
        }
    }
}