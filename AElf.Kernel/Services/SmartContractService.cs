using System.Threading.Tasks;
using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class SmartContractService : ISmartContractService
    {
        private IBlockManager _blockManager;

        public SmartContractService(IBlockManager blockManager)
        {
            _blockManager = blockManager;
        }

        public async Task<ISmartContract> GetAsync(Hash account, IChainContext context)
        {
            var sm = context.SmartContractZero;

            if (account == Hash.Zero)
            {
                return sm;
            }

            return await sm.GetSmartContractAsync(account);

        }
    }
}