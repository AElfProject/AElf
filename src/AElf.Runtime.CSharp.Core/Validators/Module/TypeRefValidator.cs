using System.Collections.Generic;
using System.Data;
using System.Linq;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Validators.Module
{
    public class TypeRefValidator : IValidator<ModuleDefinition>
    {
        private readonly HashSet<AccessRule> rules;
        
        public TypeRefValidator(HashSet<AccessRule> rules)
        {
            this.rules = rules;
        }

        public IEnumerable<ValidationResult> Validate(ModuleDefinition module)
        {
            // Check if there is any unknown reference
            var disallowed = module.GetTypeReferences().Where(tr => !rules.Any(r => r.IsAllowed(tr.FullName)));
            var errors = new List<ValidationResult>();
            
            if (disallowed.Any())
            {
                foreach (var typRef in disallowed)
                {
                    errors.Add(new TypeRefValidationResult("Type reference " + typRef.FullName + " is not allowed."));
                }
            }

            return errors;
        }
    }
    
    public class TypeRefValidationResult : ValidationResult
    {
        public TypeRefValidationResult(string message) : base(message)
        {
        }
    }
}