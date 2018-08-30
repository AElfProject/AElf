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

        private List<IAElfModlule> _modlules;

        public AElfModuleHandler()
        {
            _builder = new ContainerBuilder();
            _modlules = new List<IAElfModlule>();
        }

        public void Register(IAElfModlule modlule)
        {
            _modlules.Add(modlule);
        }

        public void Build()
        {
            _modlules.ForEach(m => m.Init(_builder));

            _container = _builder.Build();
            if (_container == null)
            {
                throw new Exception("IoC setup failed");
            }

             using (var scope = _container.BeginLifetimeScope())
            {
                _modlules.ForEach(m => m.Run(scope));
            }
        }
    }
}