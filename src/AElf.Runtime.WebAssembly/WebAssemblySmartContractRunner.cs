using AElf.Kernel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Solang;
using Volo.Abp.DependencyInjection;

namespace AElf.Runtime.WebAssembly;

public class WebAssemblySmartContractRunner : ISmartContractRunner, ISingletonDependency
{
    public int Category { get; protected set; } = KernelConstants.SolidityRunnerCategory;

    public string ContractVersion { get; protected set; } = string.Empty;

    public async Task<IExecutive> RunAsync(SmartContractRegistration reg)
    {
        try
        {
            var code = reg.Code.ToByteArray();
            var output = new Compiler().BuildWasm(code);
            var executive = new Executive(output.Contracts.First());
            return await Task.FromResult(executive);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}