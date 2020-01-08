using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;

namespace AElf.CSharp.CodeOps.Validators.Assembly
{
    public class RequiredAcsDto
    {
        public bool RequireAll;
        public List<string> AcsList;
    }
    public class AcsValidator
    {
        public IEnumerable<ValidationResult> Validate(System.Reflection.Assembly assembly, RequiredAcsDto requiredAcs)
        {
            if (requiredAcs.AcsList.Count == 0)
                return Enumerable.Empty<ValidationResult>(); // No ACS required
            
            var acsBaseList = GetServiceDescriptorIdentities(GetServerServiceDefinition(assembly));
            
            if (requiredAcs.RequireAll)
            {
                // Contract should have all listed ACS as a base
                if (!acsBaseList.All(a => requiredAcs.AcsList.Contains(a)))
                    return new List<ValidationResult>
                    {
                        new AcsValidationResult($"Contract should have at least {string.Join(" or ", requiredAcs.AcsList)} as base.")
                    };                
            }
            else
            {
                // Contract should have at least one of the listed ACS in the list as a base
                if (!acsBaseList.Any(a => requiredAcs.AcsList.Contains(a)))
                    return new List<ValidationResult>
                    {
                        new AcsValidationResult($"Contract should have all {string.Join(", ", requiredAcs.AcsList)} as base.")
                    };
            }

            return Enumerable.Empty<ValidationResult>();
        }

        private static ServerServiceDefinition  GetServerServiceDefinition(System.Reflection.Assembly assembly)
        {
            var methodInfo = assembly.FindContractContainer()
                .GetMethod("BindService", new[] { assembly.FindContractBaseType() });
            
            var serviceDefinition = methodInfo.Invoke(null, new[]
            {
                Activator.CreateInstance(assembly.FindContractType())
            }) as ServerServiceDefinition;

            return serviceDefinition;
        }

        private static IEnumerable<string> GetServiceDescriptorIdentities(ServerServiceDefinition serviceDefinition)
        {
            var binder = new DescriptorOnlyServiceBinder();
            serviceDefinition.BindService(binder);
            return binder.GetDescriptors()
                .Select(service => service.File.GetIndentity());
        }
    }
    
    public class AcsValidationResult : ValidationResult
    {
        public AcsValidationResult(string message) : base(message)
        {
        }
    }
}
