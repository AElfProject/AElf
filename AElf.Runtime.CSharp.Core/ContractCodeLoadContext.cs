using System.Reflection;
using System.Runtime.Loader;

namespace AElf.Runtime.CSharp
{
    /// <summary>
    ///     Smart contract running context which contains the contract assembly with a unique Api singleton.
    /// </summary>
    public class ContractCodeLoadContext : AssemblyLoadContext
    {
        private readonly ISdkStreamManager _sdkStreamManager;

        public ContractCodeLoadContext(ISdkStreamManager sdkStreamManager)
        {
            _sdkStreamManager = sdkStreamManager;
        }

        public Assembly Sdk { get; private set; }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            return LoadFromFolderOrDefault(assemblyName);
        }

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
    }
}