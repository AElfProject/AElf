using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class AElfDefaultConventionalRegistrar : DefaultConventionalRegistrar
    {
        private readonly List<string> _transientTypeSuffixes =
            new List<string> {"Service", "Provider", "Manager", "Store", "Factory"};
        
        protected override ServiceLifetime? GetServiceLifetimeFromClassHierarcy(Type type)
        {
            var lifeTime = base.GetServiceLifetimeFromClassHierarcy(type);
            if (lifeTime != null)
            {
                return null;
            }

            var interfaceName = "I" + type.Name;

            if (type.GetInterfaces().Any(p => p.Name == interfaceName))
            {
                if (_transientTypeSuffixes.Any(suffix => type.Name.EndsWith(suffix)))
                {
                    return ServiceLifetime.Transient;
                }
            }


            return null;
        }
    }
}