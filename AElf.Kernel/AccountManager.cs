using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        public Task<IAccount> CreateAccountAsync(byte[] smartContract, IChain chain)
        {
            throw new System.NotImplementedException();
        }

        public IAccount GetAccountByHash(IHash<IAccount> hash)
        {
            throw new System.NotImplementedException();
        }

    }
}
