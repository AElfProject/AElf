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
        
        public DefaultPolicy()
        {
            Whitelist = new Whitelist()
                // Allowed namespaces
                .Namespace("AElf.Sdk", Permission.Allowed)
                .Namespace("AElf.CSharp.Core", Permission.Allowed)
                .Namespace("AElf.Kernel", Permission.Allowed)
                .Namespace("Google.Protobuf", Permission.Allowed)
                .Namespace("System.Collections.Generic", Permission.Allowed)
            
                // Selectively allowed types and members
                .Namespace("System", Permission.Denied, type => type
                    .Type(nameof(DateTime), Permission.Allowed, member => member
                        .Member(nameof(DateTime.Now), Permission.Denied)
                        .Member(nameof(DateTime.UtcNow), Permission.Denied)
                        .Member(nameof(DateTime.Today), Permission.Denied))
                    .Type(typeof(void).Name, Permission.Allowed)
                    .Type(typeof(object).Name, Permission.Allowed)
                    // Primitive types
                    .Type(typeof(bool).Name, Permission.Allowed)
                    .Type(typeof(byte).Name, Permission.Allowed)
                    .Type(typeof(sbyte).Name, Permission.Allowed)
                    .Type(typeof(char).Name, Permission.Allowed)
                    .Type(typeof(int).Name, Permission.Allowed)
                    .Type(typeof(uint).Name, Permission.Allowed)
                    .Type(typeof(long).Name, Permission.Allowed)
                    .Type(typeof(ulong).Name, Permission.Allowed)
                    .Type(typeof(string).Name, Permission.Allowed)
                    .Type(typeof(Byte[]).Name, Permission.Allowed))
                .Namespace("System.Reflection", Permission.Denied, type => type
                    .Type(nameof(AssemblyCompanyAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyConfigurationAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyFileVersionAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyInformationalVersionAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyProductAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyTitleAttribute), Permission.Allowed));
            
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