using System;
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
            
            //TODO! use IsAssignableFrom
            
            
            
            if (type.Name.EndsWith("Factory"))
            {
                return ServiceLifetime.Transient;
            }
            if (type.Name.EndsWith("Store"))
            {
                return ServiceLifetime.Transient;
            }
            if (type.Name.EndsWith("Info"))
            {
                return ServiceLifetime.Transient;
            }
            
            if (type.Name.EndsWith("Manager") || type.Name.EndsWith("Store") || type.Name.EndsWith("Service"))
            {
                return ServiceLifetime.Transient;
            }

            return null;
        }
        
        /*private static bool IsPageModel(Type type)
        {
            return typeof(PageModel).IsAssignableFrom(type) || type.IsDefined(typeof(PageModelAttribute), true);
        }*/
    }
}