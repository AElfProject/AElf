using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Storages;

namespace AElf.Kernel
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

    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash chainId, Hash account);
        Task<SmartContractRegistration> InsertAsync(SmartContractRegistration reg);
    }

    public class SmartContractManager : ISmartContractManager
    {
        private readonly ISmartContractRegistrationStore _smartContractRegisterationStore;

        public SmartContractManager(ISmartContractRegistrationStore smartContractRegisterationStore)
        {
            _smartContractRegisterationStore = smartContractRegisterationStore;
        }

        public async Task<SmartContractRegistration> GetAsync(Hash chainId, Hash account)
        {
            return await _smartContractRegisterationStore.GetAsync(chainId, account);
        }

        public async Task<SmartContractRegistration> InsertAsync(SmartContractRegistration reg)
        {
            await _smartContractRegisterationStore.InsertAsync(reg);
            return reg;
        }
    }
}