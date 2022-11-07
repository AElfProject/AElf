using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Runtime.CSharp;

public class CSharpSmartContractRunner : ISmartContractRunner
{
    private readonly ISdkStreamManager _sdkStreamManager;

    public CSharpSmartContractRunner(
        string sdkDir)
    {
        var sdkDir1 = Path.GetFullPath(sdkDir);
        _sdkStreamManager = new SdkStreamManager(sdkDir1);
    }

    public int Category { get; protected set; }

    public virtual async Task<IExecutive> RunAsync(SmartContractRegistration reg)
    {
        var code = reg.Code.ToByteArray();

        var loadContext = GetLoadContext();

        var assembly = LoadAssembly(code, loadContext);

        if (assembly == null) throw new InvalidAssemblyException("Invalid binary code.");

        ContractVersion = assembly.GetName().Version?.ToString();

        var executive = new Executive(assembly)
        {
            ContractHash = reg.CodeHash,
            ContractVersion = ContractVersion
        };

        // AssemblyLoadContext needs to be called after initializing the Executive
        // to ensure that it is not unloaded early in release mode.
        loadContext.Unload();

        return await Task.FromResult(executive);
    }

    public string ContractVersion { get; protected set; }

    /// <summary>
    ///     Creates an isolated context for the smart contract residing with an Api singleton.
    /// </summary>
    /// <returns></returns>
    protected virtual AssemblyLoadContext GetLoadContext()
    {
        // To make sure each smart contract resides in an isolated context with an Api singleton
        return new ContractCodeLoadContext(_sdkStreamManager);
    }

    protected virtual Assembly LoadAssembly(byte[] code, AssemblyLoadContext loadContext)
    {
        using Stream stream = new MemoryStream(code);
        var assembly = loadContext.LoadFromStream(stream);
        return assembly;
    }
}