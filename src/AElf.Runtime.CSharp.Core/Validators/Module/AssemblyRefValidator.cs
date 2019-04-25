using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Validators.Module
{
    public class AssemblyRefValidator : IValidator<ModuleDefinition>
    {
        private readonly HashSet<Assembly> allowedAssemblies;
        
        public AssemblyRefValidator(HashSet<Assembly> allowed)
        {
            allowedAssemblies = allowed;
        }

        public IEnumerable<ValidationResult> Validate(ModuleDefinition module)
        {
            var errors = new List<ValidationResult>();

            foreach (var asmRef in module.AssemblyReferences)
            {
                if (!allowedAssemblies.Any(allowed => allowed.FullName == asmRef.FullName))
                    errors.Add(new AssemblyValidationResult("Assembly " + asmRef.FullName + "is not allowed."));
            }

            return errors;
        }
    }

    public class AssemblyValidationResult : ValidationResult
    {
        public AssemblyValidationResult(string message) : base(message)
        {
        }
    }
}