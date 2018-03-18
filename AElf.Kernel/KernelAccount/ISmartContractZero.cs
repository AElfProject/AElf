using System.Threading.Tasks;

namespace AElf.Kernel.KernelAccount
{
    public interface ISmartContractZero : ISmartContract
    {
        Task RegisterSmartContract(SmartContractRegistration reg);
        
        /// <summary>
        /// get smartcontract using account hash
        /// </summary>
        /// <param name="hash"></param>
        /// <returns></returns>
        Task<ISmartContract> GetSmartContractAsync(IHash hash);
    }
}