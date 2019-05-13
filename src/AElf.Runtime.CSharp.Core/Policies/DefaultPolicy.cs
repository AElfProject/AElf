using System;
using AElf.Sdk.CSharp;
using System.Reflection;
using AElf.CSharp.Core;
using AElf.Runtime.CSharp.Validators;
using Mono.Cecil;

using AElf.Runtime.CSharp.Validators.Method;
using AElf.Runtime.CSharp.Validators.Whitelist;
using AElf.Types;


namespace AElf.Runtime.CSharp.Policies
{
    public class DefaultPolicy : AbstractPolicy
    {        
        public DefaultPolicy()
        {
            Whitelist = new Whitelist()
                // Allowed assemblies
                .Assembly(Assembly.Load("netstandard"), Trust.Partial)
                .Assembly(Assembly.Load("Google.Protobuf"), Trust.Full)
                .Assembly(typeof(CSharpSmartContract).Assembly, Trust.Full) // AElf.Sdk.CSharp
                .Assembly(typeof(Address).Assembly, Trust.Full)             // AElf.Types
                .Assembly(typeof(IMethod).Assembly, Trust.Full)             // AElf.CSharp.Core
                    
                // Allowed namespaces
                //.Namespace("Aelf", Permission.Allowed) // For protobuf generated code
                //.Namespace("AElf", Permission.Allowed)
                //.Namespace("AElf.CrossChain.*", Permission.Allowed)
                //.Namespace("AElf.Sdk.*", Permission.Allowed)
                //.Namespace("AElf.CSharp.Core", Permission.Allowed)
                //.Namespace("AElf.Kernel", Permission.Allowed)        // Remove later
                //.Namespace("AElf.Kernel.Types", Permission.Allowed)  // Remove later
                //.Namespace("Google.Protobuf.*", Permission.Allowed)
                .Namespace("System.Collections.Generic", Permission.Allowed)
            
                // Selectively allowed types and members
                .Namespace("System", Permission.Denied, type => type
                    .Type("Func`1", Permission.Allowed) // Required for protobuf generated code
                    .Type("Func`2", Permission.Allowed) // Required for protobuf generated code
                    .Type("Func`3", Permission.Allowed) // Required for protobuf generated code
                    // Required to support yield keyword in protobuf generated code
                    .Type(typeof(Environment), Permission.Denied, member => member
                        .Member(nameof(Environment.CurrentManagedThreadId), Permission.Allowed))
                    .Type(typeof(NotImplementedException), Permission.Allowed) // Required for protobuf generated code
                    .Type(typeof(NotSupportedException), Permission.Allowed)   // Required for protobuf generated code
                    .Type(nameof(DateTime), Permission.Allowed, member => member
                        .Member(nameof(DateTime.Now), Permission.Denied)
                        .Member(nameof(DateTime.UtcNow), Permission.Denied)
                        .Member(nameof(DateTime.Today), Permission.Denied))
                    .Type(typeof(void).Name, Permission.Allowed)
                    .Type(typeof(object).Name, Permission.Allowed)
                    .Type(typeof(Type).Name, Permission.Allowed)
                    .Type(typeof(IDisposable).Name, Permission.Allowed)
                    .Type(typeof(Convert).Name, Permission.Allowed)
                    .Type(typeof(Math).Name, Permission.Allowed)
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
                .Namespace("System.Linq", Permission.Allowed)
                .Namespace("System.Collections", Permission.Allowed)
                .Namespace("System.Reflection", Permission.Denied, type => type
                    .Type(nameof(AssemblyCompanyAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyConfigurationAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyFileVersionAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyInformationalVersionAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyProductAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyTitleAttribute), Permission.Allowed));
            
            MethodValidators.AddRange(new IValidator<MethodDefinition>[]{
                new FloatOpsValidator(),
                new MultiDimArrayValidator(), // Should we keep this?
                // new UnsafeMathValidator(), // Google protobuf generated code contains unsafe opcodes, 
                // new NewObjValidator(),     // Define a blacklist of objects types
            });
        }
    }
}