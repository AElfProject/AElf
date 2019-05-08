using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel
{
    public class AElfKernelConventionalRegistrar : DefaultConventionalRegistrar
    {
        protected override ServiceLifetime? GetServiceLifetimeFromClassHierarcy(Type type)
        {
            var lifeTime = base.GetServiceLifetimeFromClassHierarcy(type);
            if (lifeTime != null)
            {
                return lifeTime;
            }
            
            var interfaceName = "I" + type.Name;
            
            if (type.GetInterfaces().Any(p => p.Name == interfaceName))
            {
                
                if (type.Name.EndsWith("Factory"))
                {
                    return ServiceLifetime.Transient;
                }
            
                if (type.Name.EndsWith("Manager") || type.Name.EndsWith("Store") || type.Name.EndsWith("Service"))
                {
                    return ServiceLifetime.Transient;
                }
            }

            return null;
        }
        
    }
}