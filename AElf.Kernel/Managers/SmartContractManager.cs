using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
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