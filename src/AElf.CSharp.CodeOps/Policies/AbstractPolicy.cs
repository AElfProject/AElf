using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using AElf.CSharp.CodeOps.Validators;
using AElf.CSharp.CodeOps.Validators.Whitelist;
using Mono.Cecil;

namespace AElf.CSharp.CodeOps.Policies
{
    public abstract class AbstractPolicy
    {
        public Whitelist Whitelist;
        public readonly List<IValidator<MethodDefinition>> MethodValidators;
        public readonly List<IValidator<TypeDefinition>> TypeValidators;
        public readonly List<IValidator<ModuleDefinition>> ModuleValidators;
        public readonly List<IValidator<Assembly>> AssemblyValidators;

        protected AbstractPolicy()
        {
            MethodValidators = new List<IValidator<MethodDefinition>>();

            TypeValidators = new List<IValidator<TypeDefinition>>();
            
            ModuleValidators = new List<IValidator<ModuleDefinition>>();
            
            AssemblyValidators = new List<IValidator<Assembly>>();
        }

        protected AbstractPolicy(List<AbstractPolicy> policies) : this()
        {
            policies.ForEach(p =>
            {
                MethodValidators.AddRange(p.MethodValidators);
                TypeValidators.AddRange(p.TypeValidators);
                ModuleValidators.AddRange(p.ModuleValidators);
                AssemblyValidators.AddRange(p.AssemblyValidators);
            });
        }
    }
}