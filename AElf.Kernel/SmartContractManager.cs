using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class SmartContractManager:ISmartContractManager
    {
        private IBlockManager _blockManager;
        
        public SmartContractManager(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }


        public async Task<ISmartContract> GetAsync(IAccount account,IChainContext context)
        {
            var address = account.GetAddress();

            var sm = context.SmartContractZero;
            
            if (address == Hash.Zero)
            {
                return sm;
            }

            return await sm.GetSmartContractAsync(address);
        }
    }
}