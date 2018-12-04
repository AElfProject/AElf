using System;
using AElf.CLI2.JS;
using Autofac;

namespace AElf.CLI2.Commands
{
    public abstract class Command : IDisposable
    {
        protected ILifetimeScope _scope;
        protected IJSEngine _engine;

        public Command(BaseOption option)
        {
            _scope = IoCContainerBuilder.Build(option);
            _engine = _scope.Resolve<IJSEngine>();
        }

        public abstract void Execute();

        public virtual void Dispose()
        {
            _scope.Dispose();
        }
    }
}