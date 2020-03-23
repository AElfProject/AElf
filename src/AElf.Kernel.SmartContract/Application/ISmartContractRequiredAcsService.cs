using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public class RequiredAcs
    {
        public bool RequireAll;
        public List<string> AcsList;
    }
    
    public interface ISmartContractRequiredAcsService
    {
        Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight);
    }
}