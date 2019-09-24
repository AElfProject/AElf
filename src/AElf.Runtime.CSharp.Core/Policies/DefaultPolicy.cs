using System;
using System.Globalization;
using AElf.Sdk.CSharp;
using System.Reflection;
using System.Runtime.CompilerServices;
using AElf.Cryptography.SecretSharing;
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
            Whitelist = new Whitelist();
            
            WhitelistAssemblies();
            WhitelistSystemTypes();
            WhitelistReflectionTypes();
            WhitelistLinqAndCollections();
            WhitelistOthers();

            UseMethodValidators();
        }

        private void WhitelistAssemblies()
        {
            Whitelist
                .Assembly(Assembly.Load("netstandard"), Trust.Partial)
                .Assembly(Assembly.Load("System.Runtime"), Trust.Partial)
                .Assembly(Assembly.Load("System.Runtime.Extensions"), Trust.Partial)
                .Assembly(Assembly.Load("System.Private.CoreLib"), Trust.Partial)
                .Assembly(Assembly.Load("System.ObjectModel"), Trust.Partial)
                .Assembly(Assembly.Load("System.Linq"), Trust.Full)
                .Assembly(Assembly.Load("System.Collections"), Trust.Full)
                .Assembly(Assembly.Load("Google.Protobuf"), Trust.Full)
                
                .Assembly(typeof(CSharpSmartContract).Assembly, Trust.Full) // AElf.Sdk.CSharp
                .Assembly(typeof(Address).Assembly, Trust.Full) // AElf.Types
                .Assembly(typeof(IMethod).Assembly, Trust.Full) // AElf.CSharp.Core
                .Assembly(typeof(SecretSharingHelper).Assembly, Trust.Full) // AElf.Cryptography
                ;
        }

        private void WhitelistSystemTypes()
        {
            Whitelist
                // Selectively allowed types and members
                .Namespace("System", Permission.Denied, type => type
                    .Type("Func`1", Permission.Allowed) // Required for protobuf generated code
                    .Type("Func`2", Permission.Allowed) // Required for protobuf generated code
                    .Type("Func`3", Permission.Allowed) // Required for protobuf generated code
                    .Type("Nullable`1", Permission.Allowed) // Required for protobuf generated code
                    // Required to support yield keyword in protobuf generated code
                    .Type(typeof(Environment), Permission.Denied, member => member
                        .Member(nameof(Environment.CurrentManagedThreadId), Permission.Allowed))
                    .Type(typeof(BitConverter), Permission.Denied, member => member
                        .Member(nameof(BitConverter.GetBytes), Permission.Allowed))
                    .Type(typeof(NotImplementedException),
                        Permission.Allowed) // Required for protobuf generated code
                    .Type(typeof(NotSupportedException), Permission.Allowed) // Required for protobuf generated code
                    .Type(typeof(ArgumentOutOfRangeException), Permission.Allowed) // From AEDPoS
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
                    .Type(typeof(decimal).Name, Permission.Allowed)
                    .Type(typeof(string).Name, Permission.Allowed, member => member
                        .Constructor(Permission.Denied)
                        .Member(nameof(string.Concat), Permission.Denied))
                    .Type(typeof(Byte[]).Name, Permission.Allowed)
                );
        }

        private void WhitelistReflectionTypes()
        {
            Whitelist
                // Used by protobuf generated code
                .Namespace("System.Reflection", Permission.Denied, type => type
                    .Type(nameof(AssemblyCompanyAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyConfigurationAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyFileVersionAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyInformationalVersionAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyProductAttribute), Permission.Allowed)
                    .Type(nameof(AssemblyTitleAttribute), Permission.Allowed))
                ;
        }

        private void WhitelistLinqAndCollections()
        {
            Whitelist
                .Namespace("System.Linq", Permission.Allowed)
                .Namespace("System.Collections", Permission.Allowed)
                .Namespace("System.Collections.Generic", Permission.Allowed)
                ;
        }

        private void WhitelistOthers()
        {
            Whitelist
                // Used for converting numbers to strings
                .Namespace("System.Globalization", Permission.Denied, type => type
                    .Type(nameof(CultureInfo), Permission.Denied, m => m
                        .Member(nameof(CultureInfo.InvariantCulture), Permission.Allowed)))
                
                // Used for initializing large arrays hardcoded in the code, array validator will take care of the size
                .Namespace("System.Runtime.CompilerServices", Permission.Denied, type => type
                    .Type(nameof(RuntimeHelpers), Permission.Denied, member => member
                        .Member(nameof(RuntimeHelpers.InitializeArray), Permission.Allowed)))
                ;
        }

        private void UseMethodValidators()
        {
            MethodValidators.AddRange(new IValidator<MethodDefinition>[]{
                new FloatOpsValidator(),
                new ArrayValidator(), 
                new MultiDimArrayValidator(),
                // TODO: Enable unchecked math validator once test cases are passing with overflow check
                // new UncheckedMathValidator(),
            });
        }
    }
}