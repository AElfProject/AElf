using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract;

namespace AElf.CSharp.CodeOps.Validators.Assembly
{
    public interface IAcsValidator
    {
        IEnumerable<ValidationResult> Validate(System.Reflection.Assembly assembly, RequiredAcs requiredAcs);
    }
    
    public class AcsValidator : IAcsValidator
    {
        public IEnumerable<ValidationResult> Validate(System.Reflection.Assembly assembly, RequiredAcs requiredAcs)
        {
            if (requiredAcs.AcsList.Count == 0)
                return Enumerable.Empty<ValidationResult>(); // No ACS required

            var acsBaseList = GetServiceDescriptorIdentities(GetServerServiceDefinition(assembly));

            if (requiredAcs.RequireAll)
            {
                // Contract should have all listed ACS as a base
                if (requiredAcs.AcsList.Any(acs => !acsBaseList.Contains(acs)))
                    return new List<ValidationResult>
                    {
                        new AcsValidationResult(
                            $"Contract should have all {string.Join(", ", requiredAcs.AcsList)} as base.")
                    };
            }
            else
            {
                // Contract should have at least one of the listed ACS in the list as a base
                if (requiredAcs.AcsList.All(acs => !acsBaseList.Contains(acs)))
                    return new List<ValidationResult>
                    {
                        new AcsValidationResult(
                            $"Contract should have at least {string.Join(" or ", requiredAcs.AcsList)} as base.")
                    };
            }

            return Enumerable.Empty<ValidationResult>();
        }

        private static ServerServiceDefinition GetServerServiceDefinition(System.Reflection.Assembly assembly)
        {
            var methodInfo = assembly.FindContractContainer()
                .GetMethod("BindService", new[] {assembly.FindContractBaseType()});

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
                .Select(service => service.File.GetIdentity());
        }
    }

    public class AcsValidationResult : ValidationResult
    {
        public AcsValidationResult(string message) : base(message)
        {
        }
    }
}