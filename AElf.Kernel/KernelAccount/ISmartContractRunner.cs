using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractRunner
    {
        Task<ISmartContract> RunAsync(SmartContractRegistration reg);
    }
    
    public class KernelZeroSmartContractRunner: ISmartContractRunner
    {
        public async Task<ISmartContract> RunAsync(SmartContractRegistration reg)
        {
            throw new System.NotImplementedException();
        }
    }
}