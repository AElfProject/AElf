using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class SmartContractManager:ISmartContractManager
    {
        public async Task<ISmartContract> GetAsync(IAccount account)
        {
            var address = account.GetAddress();
            if (address == Hash<IAccount>.Zero)
            {
                
            }
        }
    }
}