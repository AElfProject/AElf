using System;
using AElf.Sdk.CSharp;
using System.Collections.Generic;
using System.Reflection;
using AElf.CSharp.Core;
using AElf.Runtime.CSharp.Validators;
using Mono.Cecil;

using AElf.Runtime.CSharp.Validators.Method;
using AElf.Runtime.CSharp.Validators.Module;
using AElf.Runtime.CSharp.Validators.Whitelist;


namespace AElf.Runtime.CSharp.Policies
{
    public class DefaultPolicy : AbstractPolicy
    {
        private static readonly HashSet<Assembly> AllowedAssemblies = new HashSet<Assembly> {
            Assembly.Load("netstandard"),
            Assembly.Load("Google.Protobuf"),
            
            // AElf dependencies
            typeof(CSharpSmartContract).Assembly, // AElf.Sdk.CSharp
            typeof(IMethod).Assembly,             // AElf.CSharp.Core
        };
        
        public static Whitelist Whitelist = new Whitelist()
            // Allowed namespaces
            .Namespace("AElf.Sdk", Permission.Allow)
            .Namespace("AElf.CSharp.Core", Permission.Allow)
            .Namespace("AElf.Kernel", Permission.Allow)
            .Namespace("Google.Protobuf", Permission.Allow)
            
            // Selectively allowed types and members
            .Namespace("System", Permission.Deny, type => type
                .Type(nameof(DateTime), Permission.Allow, member => member
                    .Member(nameof(DateTime.Now), Permission.Deny)
                    .Member(nameof(DateTime.UtcNow), Permission.Deny)
                    .Member(nameof(DateTime.Today), Permission.Deny)))
            .Namespace("System.Reflection", Permission.Deny, type => type
                .Type(nameof(AssemblyCompanyAttribute), Permission.Allow)
                .Type(nameof(AssemblyConfigurationAttribute), Permission.Allow)
                .Type(nameof(AssemblyFileVersionAttribute), Permission.Allow)
                .Type(nameof(AssemblyInformationalVersionAttribute), Permission.Allow)
                .Type(nameof(AssemblyProductAttribute), Permission.Allow)
                .Type(nameof(AssemblyTitleAttribute), Permission.Allow));
        
        public DefaultPolicy()
        {
            ModuleValidators.AddRange(new IValidator<ModuleDefinition>[]
            {
                new AssemblyRefValidator(AllowedAssemblies), 
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