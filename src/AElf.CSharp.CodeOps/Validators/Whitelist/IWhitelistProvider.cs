using System;
using System.Globalization;
using AElf.Sdk.CSharp;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using AElf.Cryptography.SecretSharing;
using AElf.CSharp.Core;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.CSharp.CodeOps.Validators.Whitelist;

public interface IWhitelistProvider
{
    Whitelist GetWhitelist();
}

public class WhitelistProvider : IWhitelistProvider
{
    private Whitelist _whitelist;

    public Whitelist GetWhitelist()
    {
        if (_whitelist != null)
            return _whitelist;
        _whitelist = CreateWhitelist();
        return _whitelist;
    }

    private void WhitelistAssemblies(Whitelist whitelist)
    {
        whitelist
            .Assembly(System.Reflection.Assembly.Load("netstandard"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Runtime"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Runtime.Extensions"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Private.CoreLib"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.ObjectModel"), Trust.Partial)
            .Assembly(System.Reflection.Assembly.Load("System.Linq"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Linq.Expressions"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("System.Collections"), Trust.Full)
            .Assembly(System.Reflection.Assembly.Load("Google.Protobuf"), Trust.Full)
            .Assembly(typeof(CSharpSmartContract).Assembly, Trust.Full) // AElf.Sdk.CSharp
            .Assembly(typeof(Address).Assembly, Trust.Full) // AElf.Types
            .Assembly(typeof(IMethod).Assembly, Trust.Full) // AElf.CSharp.Core
            .Assembly(typeof(SecretSharingHelper).Assembly, Trust.Partial) // AElf.Cryptography
            .Assembly(typeof(ISmartContractBridgeContext).Assembly, Trust.Full) // AElf.Kernel.SmartContract.Shared
            ;
    }

    private void WhitelistSystemTypes(Whitelist whitelist)
    {
        whitelist
            // Selectively allowed types and members
            .Namespace("System", Permission.Denied, type => type
                .Type(typeof(Array), Permission.Denied, member => member
                    .Member(nameof(Array.AsReadOnly), Permission.Allowed))
                .Type("Func`1", Permission.Allowed) // Required for protobuf generated code
                .Type("Func`2", Permission.Allowed) // Required for protobuf generated code
                .Type("Func`3", Permission.Allowed) // Required for protobuf generated code
                .Type("Nullable`1", Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(BitConverter), Permission.Denied, member => member
                    .Member(nameof(BitConverter.GetBytes), Permission.Allowed))
                .Type(typeof(Uri), Permission.Denied, member => member
                    .Member(nameof(Uri.TryCreate), Permission.Allowed)
                    .Member(nameof(Uri.Scheme), Permission.Allowed)
                    .Member(nameof(Uri.UriSchemeHttp), Permission.Allowed)
                    .Member(nameof(Uri.UriSchemeHttps), Permission.Allowed))
                .Type(typeof(NotImplementedException),
                    Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(NotSupportedException), Permission.Allowed) // Required for protobuf generated code
                .Type(typeof(ArgumentOutOfRangeException), Permission.Allowed) // From AEDPoS
                .Type(nameof(DateTime), Permission.Allowed, member => member
                    .Member(nameof(DateTime.Now), Permission.Denied)
                    .Member(nameof(DateTime.UtcNow), Permission.Denied)
                    .Member(nameof(DateTime.Today), Permission.Denied))
                .Type(typeof(void).Name, Permission.Allowed)
                .Type(nameof(Object), Permission.Allowed)
                .Type(nameof(Type), Permission.Allowed)
                .Type(nameof(IDisposable), Permission.Allowed)
                .Type(nameof(Convert), Permission.Allowed)
                .Type(nameof(Math), Permission.Allowed)
                // Primitive types
                .Type(nameof(Boolean), Permission.Allowed)
                .Type(nameof(Byte), Permission.Allowed)
                .Type(nameof(SByte), Permission.Allowed)
                .Type(nameof(Char), Permission.Allowed)
                .Type(nameof(Int32), Permission.Allowed)
                .Type(nameof(UInt32), Permission.Allowed)
                .Type(nameof(Int64), Permission.Allowed)
                .Type(nameof(UInt64), Permission.Allowed)
                .Type(nameof(Decimal), Permission.Allowed)
                .Type(nameof(String), Permission.Allowed, member => member
                    .Constructor(Permission.Denied)
                    .Member(nameof(String.Concat), Permission.Denied)
                )
                .Type(typeof(Byte[]).Name, Permission.Allowed)
            );
    }

    private void WhitelistReflectionTypes(Whitelist whitelist)
    {
        whitelist
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

    private void WhitelistLinqAndCollections(Whitelist whitelist)
    {
        whitelist
            .Namespace("System.Linq", Permission.Allowed)
            .Namespace("System.Collections", Permission.Allowed)
            .Namespace("System.Collections.Generic", Permission.Allowed)
            .Namespace("System.Collections.ObjectModel", Permission.Allowed)
            ;
    }

    private void WhitelistOthers(Whitelist whitelist)
    {
        whitelist
            // Used for converting numbers to strings
            .Namespace("System.Globalization", Permission.Denied, type => type
                .Type(nameof(CultureInfo), Permission.Denied, m => m
                    .Member(nameof(CultureInfo.InvariantCulture), Permission.Allowed)))

            // Used for initializing large arrays hardcoded in the code, array validator will take care of the size
            .Namespace("System.Runtime.CompilerServices", Permission.Denied, type => type
                .Type(nameof(RuntimeHelpers), Permission.Denied, member => member
                    .Member(nameof(RuntimeHelpers.InitializeArray), Permission.Allowed))
                .Type(nameof(DefaultInterpolatedStringHandler), Permission.Allowed)
                )
            .Namespace("System.Text", Permission.Denied, type => type
                .Type(nameof(Encoding), Permission.Denied, member => member
                    .Member(nameof(Encoding.UTF8), Permission.Allowed)
                    .Member(nameof(Encoding.UTF8.GetByteCount), Permission.Allowed)))
            .Namespace("System.Numerics", Permission.Allowed)
            ;
    }

    private Whitelist CreateWhitelist()
    {
        var whitelist = new Whitelist();
        WhitelistAssemblies(whitelist);
        WhitelistSystemTypes(whitelist);
        WhitelistReflectionTypes(whitelist);
        WhitelistLinqAndCollections(whitelist);
        WhitelistOthers(whitelist);
        return whitelist;
    }
}

public interface ISystemContractWhitelistProvider : IWhitelistProvider
{
}

public class SystemContractWhitelistProvider : WhitelistProvider, ISystemContractWhitelistProvider, ISingletonDependency
{
    public Whitelist GetWhitelist()
    {
        var whitelist = base.GetWhitelist();
        WhitelistAElfTypes(whitelist);
        return whitelist;
    }

    private void WhitelistAElfTypes(Whitelist whitelist)
    {
        whitelist
            // Selectively allowed types and members
            .Namespace("AElf.Cryptography.SecretSharing", Permission.Denied, type => type
                .Type(typeof(SecretSharingHelper), Permission.Denied, member => member
                    .Member(nameof(SecretSharingHelper.DecodeSecret), Permission.Allowed)));
    }
}