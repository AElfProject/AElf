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


        /// <summary>
        /// get smartcontract with account and chain context
        /// </summary>
        /// <param name="account"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<ISmartContract> GetAsync(IAccount account, IChainContext context)
        {
            var address = account.GetAddress();

            var sm = context.SmartContractZero;
            
            if (address == Hash<IAccount>.Zero)
            {
                return sm;
            }

            return await sm.GetSmartContractAsync(address);
        }
    }
}