using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class SmartContractManager:ISmartContractManager
    {
        private ISmartContractZero _smartContractZero;
        private IWorldStateManager _worldStateManager;

        public SmartContractManager(IWorldStateManager worldStateManager)
        {
            _worldStateManager = worldStateManager;
            
        }


        public async Task<ISmartContract> GetAsync(IAccount account)
        {
            var address = account.GetAddress();
            if (address == Hash<IAccount>.Zero)
            {
                return _smartContractZero;
            }

            return await _smartContractZero.GetSmartContractAsync(address);
        }
    }
}