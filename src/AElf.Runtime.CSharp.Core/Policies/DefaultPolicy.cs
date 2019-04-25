using AElf.Sdk.CSharp;
using System.Collections.Generic;
using System.Reflection;
using AElf.CSharp.Core;
using AElf.Runtime.CSharp.Validators;
using Mono.Cecil;

using AElf.Runtime.CSharp.Validators.Method;
using AElf.Runtime.CSharp.Validators.Module;


namespace AElf.Runtime.CSharp.Policies
{
    public class DefaultPolicy : AbstractPolicy
    {
        private static readonly HashSet<Assembly> AllowedAssemblies = new HashSet<Assembly> {
            Assembly.Load("netstandard"),
            Assembly.Load("Google.Protobuf"),
            
            // AElf dependencies
            typeof(CSharpSmartContract).Assembly,
            typeof(IMethod).Assembly,
        };
        
        public DefaultPolicy()
        {
            ModuleValidators.AddRange(new []
            {
                new AssemblyRefValidator(AllowedAssemblies), 
            });
            
            MethodValidators.AddRange(new IValidator<MethodDefinition>[]{
                new FloatValidator(),
                new MultiDimArrayValidator(), 
                //new NewObjValidator(), 
            });
            
            // TypeValidators.AddRange();
            
            // Whitelist namespaces, need namespace validator
        }
    }
}