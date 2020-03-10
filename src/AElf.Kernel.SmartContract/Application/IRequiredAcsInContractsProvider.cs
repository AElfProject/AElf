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
    
    public interface IRequiredAcsInContractsProvider
    {
        Task<RequiredAcs> GetRequiredAcsInContractsAsync(Hash blockHash, long blockHeight);
    }
}