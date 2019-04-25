using System.Collections.Generic;
using System.ComponentModel;
using AElf.Runtime.CSharp.Validators;
using Mono.Cecil;

namespace AElf.Runtime.CSharp.Policies
{
    public abstract class AbstractPolicy
    {
        public readonly List<IValidator<MethodDefinition>> MethodValidators;
        public readonly List<IValidator<ModuleDefinition>> ModuleValidators;
        public readonly List<IValidator<TypeDefinition>> TypeValidators;

        protected AbstractPolicy()
        {
            MethodValidators = new List<IValidator<MethodDefinition>>();
            
            ModuleValidators = new List<IValidator<ModuleDefinition>>();
            
            TypeValidators = new List<IValidator<TypeDefinition>>();
        }

        protected AbstractPolicy(List<AbstractPolicy> policies) : this()
        {
            policies.ForEach(p =>
            {
                MethodValidators.AddRange(p.MethodValidators);
                ModuleValidators.AddRange(p.ModuleValidators);
                TypeValidators.AddRange(p.TypeValidators);
            });
        }
    }
}