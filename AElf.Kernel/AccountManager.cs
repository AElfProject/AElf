using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        public Task<IAccount> CreateAccountAsync(SmartContractRegistration registration, IChain chain)
        {
            IAccount acc = new Account(new Hash<IAccount>(registration.Hash.Value));
            return Task.FromResult(acc);
        }

        public IAccount GetAccountByHash(IHash hash)
        {
            throw new System.NotImplementedException();
        }
    }
}
