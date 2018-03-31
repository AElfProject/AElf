using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class SmartContractService:ISmartContractService
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

            return await sm.GetSmartContractAsync(account);        }
    }

    public interface ISmartContractManager
    {
        Task<SmartContractRegistration> GetAsync(Hash account);
        Task<SmartContractRegistration> InsertAsync(SmartContractRegistration reg);
    }

    public class SmartContractManager : ISmartContractManager
    {
        public Task<SmartContractRegistration> GetAsync(Hash account)
        {
            throw new System.NotImplementedException();
        }

        public Task<SmartContractRegistration> InsertAsync(SmartContractRegistration reg)
        {
            throw new System.NotImplementedException();
        }
    }
}