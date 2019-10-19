using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Reflection;
using System.Runtime.CompilerServices;
using AElf.CSharp.Core;

namespace AElf.Runtime.CSharp
{
    /// <summary>
    /// Smart contract running context which contains the contract assembly with a unique Api singleton.
    /// </summary>
    public class ContractCodeLoadContext : AssemblyLoadContext
    {
        private readonly ISdkStreamManager _sdkStreamManager;
        private Assembly Sdk;
        private Assembly _contractAssembly;

        public ContractCodeLoadContext(ISdkStreamManager sdkStreamManager) : base(isCollectible: true)
        {
            _sdkStreamManager = sdkStreamManager;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override Assembly Load(AssemblyName assemblyName)
        {
            return LoadFromFolderOrDefault(assemblyName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Assembly LoadFromFolderOrDefault(AssemblyName assemblyName)
        {
            if (assemblyName.Name.StartsWith("AElf.Sdk"))
            {
                // Sdk assembly should NOT be shared
                Sdk = LoadFromStream(_sdkStreamManager.GetStream(assemblyName));
                return Sdk;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LoadFromStream(byte[] code)
        {
            Assembly assembly = null;
            using (Stream stream = new MemoryStream(code))
            {
                assembly = LoadFromStream(stream);
            }

            if (assembly == null)
                throw new InvalidCodeException("Invalid binary code.");
            
            _contractAssembly = assembly;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type GetContractType()
        {
            return FindContractType(_contractAssembly);
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type FindContractType(Assembly assembly)
        {
            var types = _contractAssembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && !t.IsNested);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string AssemblyName() => _contractAssembly.FullName.ToString();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type FindContractBaseType() => FindContractBaseType(_contractAssembly);
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private Type FindContractBaseType(Assembly assembly)
        {
            var types = assembly.GetTypes();
            return types.SingleOrDefault(t => typeof(ISmartContract).IsAssignableFrom(t) && t.IsNested);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Type FindContractContainer()
        {
            var contractBase = FindContractBaseType(_contractAssembly);
            return contractBase.DeclaringType;
        }


    }
}