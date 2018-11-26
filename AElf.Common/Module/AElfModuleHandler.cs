using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Autofac;

namespace AElf.Common.Module
{
    public class AElfModuleHandler
    {
        private readonly ContainerBuilder _builder;
        private IContainer _container;

        private readonly List<IAElfModule> _modules;

        public AElfModuleHandler()
        {
            _builder = new ContainerBuilder();
            _modules = new List<IAElfModule>();
        }

        public void Register(IAElfModule module)
        {
            _modules.Add(module);
        }

        public void Build()
        {
            _modules.ForEach(m => m.Init(_builder));

            _container = _builder.Build();
            if (_container == null)
            {
                throw new Exception("IoC setup failed");
            }

            using (var scope = _container.BeginLifetimeScope())
            {
                _modules.ForEach(m => m.Run(scope));
            }
        }
    }
}