using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElf.CSharp.CodeOps.Validators.Module
{
    public class ObserverProxyValidator : IValidator<ModuleDefinition>
    {
        private static readonly TypeDefinition counterProxyTypeRef =
            AssemblyDefinition.ReadAssembly(typeof(ExecutionObserverProxy).Assembly.Location)
                .MainModule.Types.SingleOrDefault(t => t.Name == nameof(ExecutionObserverProxy));

        public IEnumerable<ValidationResult> Validate(ModuleDefinition item)
        {
            var errors = new List<ValidationResult>();
            
            // Get counter type
            var counterTypeInj = item.Types.Single(t => t.Name == nameof(ExecutionObserverProxy));

            errors.AddRange(CheckObserverProxyIsNotTampered(counterTypeInj));

            return errors;
        }

        private IEnumerable<ValidationResult> CheckObserverProxyIsNotTampered(TypeDefinition injectedType)
        {
            var errors = new List<ValidationResult>();
            
            if (!injectedType.HasSameFields(counterProxyTypeRef))
            {
                errors.Add(new ObserverProxyValidationResult(injectedType.Name + " type has different fields."));
            }

            foreach (var refMethod in counterProxyTypeRef.Methods)
            {
                var injMethod = injectedType.Methods.SingleOrDefault(m => m.Name == refMethod.Name);

                if (injMethod == null)
                {
                    errors.Add(new ObserverProxyValidationResult(refMethod.Name + " is not implemented in observer proxy."));
                }

                if (!injMethod.HasSameBody(refMethod))
                {
                    errors.Add(new ObserverProxyValidationResult(refMethod.Name + " proxy method body is tampered."));
                }
                
                if (!injMethod.HasSameParameters(refMethod))
                {
                    errors.Add(new ObserverProxyValidationResult(refMethod.Name + " proxy method accepts different parameters."));
                }
            }
            
            if (injectedType.Methods.Count != counterProxyTypeRef.Methods.Count)
                errors.Add(new ObserverProxyValidationResult("Observer type contains unusual number of methods."));

            return errors;
        }

        private IEnumerable<ValidationResult> CheckCallsFromContract(ModuleDefinition module)
        {
            throw new System.NotImplementedException();
        }
    }
    
    public class ObserverProxyValidationResult : ValidationResult
    {
        public ObserverProxyValidationResult(string message) : base(message)
        {
        }
    }
}