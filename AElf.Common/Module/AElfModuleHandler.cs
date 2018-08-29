using System.Collections;
using System.Collections.Generic;
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

        public void Build()
        {
            _modlules.ForEach(m => m.Init(_builder));

            _container = _builder.Build();

            _modlules.ForEach(m => m.Run(_container));
        }
    }
}