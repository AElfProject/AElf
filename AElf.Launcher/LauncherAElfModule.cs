using System;
using System.Threading;
using AElf.Common.Module;
using Autofac;

namespace AElf.Launcher
{
    public class LauncherAElfModule:IAElfModule
    {
        private int _stopped;
        private readonly AutoResetEvent _closing = new AutoResetEvent(false);
        
        public void Init(ContainerBuilder builder)
        {
        }

        public void Run(ILifetimeScope scope)
        {
            Console.CancelKeyPress += OnExit;
            _closing.WaitOne();
        }
        
        protected void OnExit(object sender, ConsoleCancelEventArgs args)
        {
            _closing.Set();
        }
    }
}