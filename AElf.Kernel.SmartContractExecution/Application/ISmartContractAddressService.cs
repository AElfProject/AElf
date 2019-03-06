using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ISmartContractAddressService
    {
        Task<Address> GetAddressByContractName(Hash name);
    }
    
    public class SmartContractAddressService: ISmartContractAddressService
    {
        public async Task<Address> GetAddressByContractName(Hash name)
        {
            
        }
    }
}