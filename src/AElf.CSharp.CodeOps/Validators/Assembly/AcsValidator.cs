using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;

namespace AElf.CSharp.CodeOps.Validators.Assembly
{
    public class AcsValidator : IValidator<System.Reflection.Assembly>
    {
        private static readonly string[] RequiredAcs = 
        {
            "acs1",
            "acs8"
        };

        public IEnumerable<ValidationResult> Validate(System.Reflection.Assembly assembly)
        {
            var acsBaseList = GetServiceDescriptorIdentities(GetServerServiceDefinition(assembly));
            
            // Contracts should have either acs1 or acs8 as a base
            // so that one of the plugins will be active at execution
            if (!acsBaseList.Any(a => RequiredAcs.Contains(a)))
                return new List<ValidationResult>
                {
                    new AcsValidationResult("Contract should have at least ACS1 or ACS8 as base.")
                };

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
