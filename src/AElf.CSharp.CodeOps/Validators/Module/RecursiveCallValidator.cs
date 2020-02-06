using System.Collections.Generic;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Validators.Module
{
    public class RecursiveCallValidator : IValidator<ModuleDefinition>
    {
        public IEnumerable<ValidationResult> Validate(ModuleDefinition item)
        {
            // No other method should be calling entry points
            
            // No entry point should be calling each other
            
            throw new System.NotImplementedException();
        }
    }
}