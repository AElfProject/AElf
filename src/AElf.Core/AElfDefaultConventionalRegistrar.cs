using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf
{
    public class AElfDefaultConventionalRegistrar : DefaultConventionalRegistrar
    {
        protected override ServiceLifetime? GetServiceLifetimeFromClassHierarcy(Type type)
        {
            //Get ABP lifetime from ABP interface, ITransientDependency,ISingletonDependency or IScopedDependency
            var lifeTime = base.GetServiceLifetimeFromClassHierarcy(type);
            if (lifeTime != null)
            {
                return lifeTime;
            }
            
            //if no lifetime interface was found, try to get class with the same interface,
            //HelloService -> IHelloService
            //HelloManager -> IHelloManager
            var interfaceName = "I" + type.Name;

            if (type.GetInterfaces().Any(p => p.Name == interfaceName))
            {
                if (type.Name.EndsWith("Manager") || type.Name.EndsWith("Service"))
                {
                    return ServiceLifetime.Transient;
                }
            }


            return null;
        }
    }
}