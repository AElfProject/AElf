using System.Reflection;
using System.Runtime.Loader;

namespace AElf.Runtime.CSharp;

/// <summary>
///     Smart contract running context which contains the contract assembly with a unique Api singleton.
/// </summary>
public class ContractCodeLoadContext : AssemblyLoadContext
{
    private readonly ISdkStreamManager _sdkStreamManager;

    public ContractCodeLoadContext(ISdkStreamManager sdkStreamManager) : base(true)
    {
        _sdkStreamManager = sdkStreamManager;
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return LoadFromFolderOrDefault(assemblyName);
    }

    /// <summary>
    ///     Will trying to load every dll located in certain dir,
    ///     but only dlls start with "AElf.Sdk" will be loaded.
    /// </summary>
    /// <param name="assemblyName"></param>
    /// <returns></returns>
    private Assembly LoadFromFolderOrDefault(AssemblyName assemblyName)
    {
        if ("AElf.Sdk.CSharp".Equals(assemblyName.Name))
        {
            // Sdk assembly should NOT be shared
            using var stream = _sdkStreamManager.GetStream(assemblyName);
            return LoadFromStream(stream);
        }

        return null;
    }
}