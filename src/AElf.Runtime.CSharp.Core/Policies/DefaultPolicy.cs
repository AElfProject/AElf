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
        
        private static readonly HashSet<AccessRule> TypeRefRules = new HashSet<AccessRule>
        {
            // Whitelisted type references
            new AccessRule("AElf.Sdk").Allow(),
            new AccessRule("AElf.CSharp.Core").Allow(),
            new AccessRule("AElf.Kernel").Allow(), // TODO: Remove when dependency cleaning is done
            new AccessRule("Google.Protobuf").Allow(),
            new AccessRule("System.Datetime").Allow(), // TODO: Disallow UtcNow, Now in method body check
            
            // Blacklisted type references with exceptions
            new AccessRule("System.Reflection").Disallow()
                                .Except("System.Reflection.AssemblyCompanyAttribute")
                                .Except("System.Reflection.AssemblyConfigurationAttribute")
                                .Except("System.Reflection.AssemblyFileVersionAttribute")
                                .Except("System.Reflection.AssemblyInformationalVersionAttribute")
                                .Except("System.Reflection.AssemblyProductAttribute")
                                .Except("System.Reflection.AssemblyTitleAttribute"),
        };
        
        public DefaultPolicy()
        {
            ModuleValidators.AddRange(new IValidator<ModuleDefinition>[]
            {
                new AssemblyRefValidator(AllowedAssemblies), 
                new TypeRefValidator(TypeRefRules), 
            });
            
            MethodValidators.AddRange(new IValidator<MethodDefinition>[]{
                new FloatValidator(),
                new MultiDimArrayValidator(),
                // new UnsafeMathValidator(), // Google protobuf generated code contains unsafe opcodes
                // new NewObjValidator(),     // Define a blacklist of objects types
            });
        }
    }
}