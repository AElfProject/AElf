using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class SmartContractManager:ISmartContractManager
    {
        private ISmartContractZero _smartContractZero;

        private IBlockManager _blockManager;
        
        public SmartContractManager(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }


        public async Task<ISmartContract> GetAsync(IAccount account,IChain chain)
        {
            var address = account.GetAddress();

            //await _blockManager.GetBlockHeaderAsync(chain.GenesisBlockHash);
            
            if (address == Hash<IAccount>.Zero)
            {
                return _smartContractZero;
            }

            return await _smartContractZero.GetSmartContractAsync(address);
        }
    }
}