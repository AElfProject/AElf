using System;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractRunner
    {
        Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment);
    }
    
    public class KernelZeroSmartContractRunner: ISmartContractRunner
    {
        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg, SmartContractDeployment deployment)
        {
            var type = Type.GetType(reg.ContractBytes.ToString());
            var paramTypes = deployment.ConstructParams.Select(p => p.Value().GetType()).ToArray();
            var constructor = type.GetConstructor(paramTypes);
            var paramArray = deployment.ConstructParams.Select(p => p.Value()).ToArray();

            var smartContract = new CSharpSmartContract
            {
                Type = type,
                Constructor = constructor,
                Params = paramArray
            };

            return smartContract;
        }
    }
}