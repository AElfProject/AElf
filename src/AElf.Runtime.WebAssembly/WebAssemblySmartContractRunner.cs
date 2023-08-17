using AElf.Kernel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Solang;

namespace AElf.Runtime.WebAssembly;

public class WebAssemblySmartContractRunner : ISmartContractRunner
{
    public int Category { get; protected set; }

    public string ContractVersion { get; protected set; } = string.Empty;

    protected IExternalEnvironment ExternalEnvironment { get; set; }

    public WebAssemblySmartContractRunner(IExternalEnvironment externalEnvironment)
    {
        ExternalEnvironment = externalEnvironment;
        Category = KernelConstants.WebAssemblyRunnerCategory;
    }

    public async Task<IExecutive> RunAsync(SmartContractRegistration reg)
    {
        var code = reg.Code.ToByteArray();
        var output = new Compiler().BuildWasm(code);
        var wasmCode = output.Contracts.First().WasmCode.ToByteArray();
        var executive = new Executive(ExternalEnvironment, wasmCode)
        {
            ContractHash = HashHelper.ComputeFrom(wasmCode),
            ContractVersion = "Unknown solidity version."
        };
        return await Task.FromResult(executive);
    }
}