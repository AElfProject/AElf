using AElf.Kernel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

public class WebAssemblySmartContractRunner : ISmartContractRunner, ISingletonDependency
{
    public int Category { get; protected set; } = KernelConstants.WasmRunnerCategory;

    public string ContractVersion { get; protected set; } = string.Empty;

    public async Task<IExecutive> RunAsync(SmartContractRegistration reg)
    {
        try
        {
            var wasmCode = new WasmContractCode();
            wasmCode.MergeFrom(reg.Code);
            var executive = new Executive(wasmCode.Abi);
            return await Task.FromResult(executive);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}